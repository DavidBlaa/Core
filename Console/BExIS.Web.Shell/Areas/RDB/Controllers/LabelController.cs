using BExIS.Dim.Entities.Mapping;
using BExIS.Dim.Helpers.Mapping;
using BExIS.Dlm.Entities.Data;
using BExIS.Dlm.Services.Data;
using BExIS.Modules.Rdb.UI.Models;
using BExIS.Xml.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using ZXing;
using ZXing.QrCode;

namespace BExIS.Modules.Rdb.UI.Controllers
{
    public class LabelController : Controller
    {
        // GET: Label
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Generate(long id, int format = 0)
        {
            DatasetManager datasetManager = new DatasetManager();

            //e.g.http://localhost:63530/rdb/Sample/Show/
            var link = Request.Url.Authority + "/rdb/Sample/Show/";

            List<LabelModel> model = new List<LabelModel>();
            try
            {

                DatasetVersion datasetVersion = datasetManager.GetDatasetLatestVersion(id);

                var titles = MappingUtils.GetValuesFromMetadata(Convert.ToInt64(Key.Title), LinkElementType.Key,
                        datasetVersion.Dataset.MetadataStructure.Id, XmlUtility.ToXDocument(datasetVersion.Metadata));

                var authors = MappingUtils.GetValuesFromMetadata(Convert.ToInt64(Key.Author), LinkElementType.Key,
                        datasetVersion.Dataset.MetadataStructure.Id, XmlUtility.ToXDocument(datasetVersion.Metadata));

                var barcodes = MappingUtils.GetValuesFromMetadata(Convert.ToInt64(Key.Barcode), LinkElementType.Key,
                        datasetVersion.Dataset.MetadataStructure.Id, XmlUtility.ToXDocument(datasetVersion.Metadata));

                var matchingObj = MappingUtils.GetValuesWithParentTypeFromMetadata(Convert.ToInt64(Key.Barcode), LinkElementType.Key,
                        datasetVersion.Dataset.MetadataStructure.Id, XmlUtility.ToXDocument(datasetVersion.Metadata));

                var types = MappingUtils.GetValuesFromMetadata(Convert.ToInt64(Key.SampleType), LinkElementType.Key,
                        datasetVersion.Dataset.MetadataStructure.Id, XmlUtility.ToXDocument(datasetVersion.Metadata));


                var plots = MappingUtils.GetValuesFromMetadata(Convert.ToInt64(Key.Plot), LinkElementType.Key,
                        datasetVersion.Dataset.MetadataStructure.Id, XmlUtility.ToXDocument(datasetVersion.Metadata));


                var sites = MappingUtils.GetValuesFromMetadata(Convert.ToInt64(Key.Site), LinkElementType.Key,
                        datasetVersion.Dataset.MetadataStructure.Id, XmlUtility.ToXDocument(datasetVersion.Metadata));

                //MappingUtils.GetAllMatchesInSystem()

                string firstTitle = titles.FirstOrDefault();
                string firstAuthor = authors.FirstOrDefault();
                string plot = plots.FirstOrDefault();
                string site = sites.FirstOrDefault();
                string type = types.FirstOrDefault();

                IBarcodeWriter writer = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE
                };


                string url = Path.Combine(link, id.ToString());
                var qrCodeWriter = new BarcodeWriterPixelData
                {
                    Format = ZXing.BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions { Height = 1000, Width = 1000, Margin = 1 }
                };

                var pixelData = qrCodeWriter.Write(url);
                Image qrcodeImage;

                using (var bitmap = new System.Drawing.Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
                using (var ms = new MemoryStream())
                {
                    var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, pixelData.Width, pixelData.Height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                    try
                    {
                        // we assume that the row stride of the bitmap is aligned to 4 byte multiplied by the width of the image
                        System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0,
                        pixelData.Pixels.Length);
                    }
                    finally
                    {
                        bitmap.UnlockBits(bitmapData);
                    }
                    // save to stream as PNG
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    qrcodeImage = Image.FromStream(ms);
                }


                foreach (var bc in matchingObj)
                {
                    string filepath;
                    //Image barcodeImage;

                    string barcodeName = "";

                    int l = bc.Value.ToString().Length;
                    if (l < 8)
                    {
                        int x = 8 - l;
                        for (int i = 0; i < x; i++)
                        {
                            barcodeName += "0";
                        }

                    }
                    barcodeName += bc.Value;

                    //barcodeImage = Code128Rendering.MakeBarcodeImage(barcodeName, 2, false);



                    LabelModel child = new LabelModel();
                    child.BarCode = null;
                    child.QRCode = qrcodeImage;
                    child.Id = id;
                    child.BarCodeText = barcodeName;
                    child.Owner = firstAuthor;
                    child.Input = firstTitle;
                    child.Plot = plot;
                    child.Site = site;
                    child.Date = DateTime.Now.ToString();
                    child.Type = type;



                    model.Add(child);
                }

                if (format == 0)
                    return PartialView("QRCodeV1A4View", model);
                else
                    return PartialView("QRCodeV2A4View", model);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                datasetManager.Dispose();
            }
        }
    }
}