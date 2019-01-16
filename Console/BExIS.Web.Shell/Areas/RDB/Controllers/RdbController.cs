using BExIS.Dim.Entities.Mapping;
using BExIS.Dim.Helpers.Mapping;
using BExIS.Dlm.Entities.Data;
using BExIS.Dlm.Services.Data;
using BExIS.Modules.Rdb.UI.Models;
using BExIS.Rdb.Helper;
using BExIS.Web.Shell.Helpers;
using BExIS.Web.Shell.Models;
using BExIS.Xml.Helpers;
using GenCode128;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using ZXing;

namespace BExIS.Modules.RDB.UI.Controllers
{
    public class RdbController : Controller
    {
        // GET: RDB/Rdb
        public ActionResult Index()
        {
            RdbImportManager importManager = new RdbImportManager();
            importManager.Load();

            Dictionary<string, EntitySelectorModel> listOfEntities = new Dictionary<string, EntitySelectorModel>();


            EntitySelectorModel bb = BexisModelManager.LoadEntitySelectorModel(importManager.TmpBoundingBoxes);
            bb.Title = "boundingboxes";
            listOfEntities.Add(bb.Title, bb);

            //sites
            EntitySelectorModel m = BexisModelManager.LoadEntitySelectorModel(importManager.Sites);
            m.Title = "Sites";
            listOfEntities.Add(m.Title, m);

            //sites
            EntitySelectorModel pperson = BexisModelManager.LoadEntitySelectorModel(importManager.Persons);
            pperson.Title = "Person";
            listOfEntities.Add(pperson.Title, pperson);


            //plots
            EntitySelectorModel p = BexisModelManager.LoadEntitySelectorModel(importManager.Plots);
            p.Title = "Plots";
            listOfEntities.Add(p.Title, p);

            //projects
            EntitySelectorModel pr = BexisModelManager.LoadEntitySelectorModel(importManager.Projects);
            pr.Title = "Projects";
            listOfEntities.Add(pr.Title, pr);

            //trees
            EntitySelectorModel t = BexisModelManager.LoadEntitySelectorModel(importManager.Trees);
            t.Title = "Trees";
            listOfEntities.Add(t.Title, t);

            RdbTestModel model = new RdbTestModel();
            model.ListOfEntites = listOfEntities;
            model.Trees = importManager.Trees;
            model.Soils = importManager.Soils;

            return View(model);
        }

        public ActionResult ConvertTo()
        {
            RdbImportManager importManager = new RdbImportManager();
            importManager.Load();
            importManager.ConvertAll();

            Dictionary<string, EntitySelectorModel> listOfEntities = new Dictionary<string, EntitySelectorModel>();

            EntitySelectorModel bb = BexisModelManager.LoadEntitySelectorModel(importManager.TmpBoundingBoxes);
            bb.Title = "boundingboxes";
            listOfEntities.Add(bb.Title, bb);

            //sites
            EntitySelectorModel m = BexisModelManager.LoadEntitySelectorModel(importManager.Sites);
            m.Title = "Sites";
            listOfEntities.Add(m.Title, m);

            //sites
            EntitySelectorModel pperson = BexisModelManager.LoadEntitySelectorModel(importManager.Persons);
            pperson.Title = "Person";
            listOfEntities.Add(pperson.Title, pperson);


            //plots
            EntitySelectorModel p = BexisModelManager.LoadEntitySelectorModel(importManager.Plots);
            p.Title = "Plots";
            listOfEntities.Add(p.Title, p);

            //projects
            EntitySelectorModel pr = BexisModelManager.LoadEntitySelectorModel(importManager.Projects);
            pr.Title = "Projects";
            listOfEntities.Add(pr.Title, pr);

            //trees
            EntitySelectorModel t = BexisModelManager.LoadEntitySelectorModel(importManager.Trees);
            t.Title = "Trees";
            listOfEntities.Add(t.Title, t);

            //Soils
            EntitySelectorModel s = BexisModelManager.LoadEntitySelectorModel(importManager.Soils);
            s.Title = "Soils";
            listOfEntities.Add(s.Title, s);

            RdbTestModel model = new RdbTestModel();
            model.ListOfEntites = listOfEntities;
            model.Trees = importManager.Trees;
            model.Soils = importManager.Soils;
            return View("Index", model);
        }

        public ActionResult GenerateBarCode(long id)
        {

            DatasetManager datasetManager = new DatasetManager();

            //e.g.http://localhost:63530/rdb/Sample/Show/
            var link = Path.Combine(Request.Url.Authority, "rdb/Sample/Show/");

            List<BarCodeLabelModel> model = new List<BarCodeLabelModel>();
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


                var plots = MappingUtils.GetValuesFromMetadata(Convert.ToInt64(Key.Plot), LinkElementType.Key,
                        datasetVersion.Dataset.MetadataStructure.Id, XmlUtility.ToXDocument(datasetVersion.Metadata));


                var sites = MappingUtils.GetValuesFromMetadata(Convert.ToInt64(Key.Site), LinkElementType.Key,
                        datasetVersion.Dataset.MetadataStructure.Id, XmlUtility.ToXDocument(datasetVersion.Metadata));

                //MappingUtils.GetAllMatchesInSystem()

                string firstTitle = titles.FirstOrDefault();
                string firstAuthor = authors.FirstOrDefault();
                string plot = plots.FirstOrDefault();
                string site = sites.FirstOrDefault();

                IBarcodeWriter writer = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE
                };

                string url = Path.Combine(link, id.ToString());

                var result = writer.Write(url);
                var barcodeBitmap = new Bitmap(result);
                Image qrcodeImage;
                qrcodeImage = barcodeBitmap;


                foreach (var bc in matchingObj)
                {
                    string filepath;
                    Image barcodeImage;

                    string barcodeName = "";
                    string type = bc.Parent;

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

                    barcodeImage = Code128Rendering.MakeBarcodeImage(barcodeName, 2, false);



                    BarCodeLabelModel child = new BarCodeLabelModel();
                    child.BarCode = barcodeImage;
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

                return PartialView("BarcodeGeneratorView", model);
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

        public ActionResult GenerateQRCode(long id)
        {

            DatasetManager datasetManager = new DatasetManager();

            //e.g.http://localhost:63530/rdb/Sample/Show/
            var link = Request.Url.Authority + "/rdb/Sample/Show/";

            List<BarCodeLabelModel> model = new List<BarCodeLabelModel>();
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


                var plots = MappingUtils.GetValuesFromMetadata(Convert.ToInt64(Key.Plot), LinkElementType.Key,
                        datasetVersion.Dataset.MetadataStructure.Id, XmlUtility.ToXDocument(datasetVersion.Metadata));


                var sites = MappingUtils.GetValuesFromMetadata(Convert.ToInt64(Key.Site), LinkElementType.Key,
                        datasetVersion.Dataset.MetadataStructure.Id, XmlUtility.ToXDocument(datasetVersion.Metadata));

                //MappingUtils.GetAllMatchesInSystem()

                string firstTitle = titles.FirstOrDefault();
                string firstAuthor = authors.FirstOrDefault();
                string plot = plots.FirstOrDefault();
                string site = sites.FirstOrDefault();

                IBarcodeWriter writer = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE
                };

                string url = Path.Combine(link, id.ToString());

                // create string for qrcode
                //StringBuilder sb = new StringBuilder();
                //sb.AppendLine(firstTitle);
                //sb.AppendLine(firstAuthor);
                //sb.AppendLine(plot);
                //sb.AppendLine(site);
                //sb.AppendLine();
                //sb.AppendLine(url);



                var result = writer.Write(url);
                var barcodeBitmap = new Bitmap(result);
                Image qrcodeImage;
                qrcodeImage = barcodeBitmap;

                foreach (var bc in matchingObj)
                {
                    string filepath;
                    //Image barcodeImage;

                    string barcodeName = "";
                    string type = bc.Parent;

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



                    BarCodeLabelModel child = new BarCodeLabelModel();
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

                return PartialView("BarcodeGeneratorView", model);
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