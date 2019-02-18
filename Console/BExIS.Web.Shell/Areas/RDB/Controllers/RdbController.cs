using BExIS.Dlm.Entities.Data;
using BExIS.Dlm.Services.Data;
using BExIS.Dlm.Services.MetadataStructure;
using BExIS.Modules.Rdb.UI.Models;
using BExIS.Rdb.Helper;
using BExIS.Web.Shell.Helpers;
using BExIS.Web.Shell.Models;
using BExIS.Xml.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Xml;

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

        public ActionResult UpdateMetadata(long id)
        {
            MetadataStructureManager metadataStructureManager = new MetadataStructureManager();
            DatasetManager datasetManager = new DatasetManager();

            try
            {

                long mdid = id;//metadataStructureManager.Repo.Get().FirstOrDefault(m => m.Name.ToLower().Equals("tree")).Id;

                List<Dataset> datasets = datasetManager.DatasetRepo.Get().Where(d => d.MetadataStructure.Id.Equals(mdid)).ToList();

                foreach (Dataset dataset in datasets)
                {

                    datasetManager.CheckOutDatasetIfNot(dataset.Id, GetUsernameOrDefault()); // there are cases, the dataset does not get checked out!!
                    if (!datasetManager.IsDatasetCheckedOutFor(dataset.Id, GetUsernameOrDefault()))
                        throw new Exception(string.Format("Not able to checkout dataset '{0}' for  user '{1}'!", dataset.Id, GetUsernameOrDefault()));

                    var workingCopy = datasetManager.GetDatasetWorkingCopy(dataset.Id);

                    workingCopy.Metadata = checkForUpdates(workingCopy.Metadata, mdid);
                    datasetManager.EditDatasetVersion(workingCopy, null, null, null);
                    datasetManager.CheckInDataset(dataset.Id, "update metadata", GetUsernameOrDefault(), ViewCreationBehavior.None);

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                metadataStructureManager.Dispose();
                datasetManager.Dispose();
            }

            return View("Index");
        }

        public string GetUsernameOrDefault()
        {
            string username = string.Empty;
            try
            {
                username = HttpContext.User.Identity.Name;
            }
            catch { }

            return !string.IsNullOrWhiteSpace(username) ? username : "DEFAULT";
        }

        //public ActionResult GenerateBarCode(long id)
        //{

        //    DatasetManager datasetManager = new DatasetManager();

        //    //e.g.http://localhost:63530/rdb/Sample/Show/
        //    var link = Path.Combine(Request.Url.Authority, "rdb/Sample/Show/");

        //    List<BarCodeLabelModel> model = new List<BarCodeLabelModel>();
        //    try
        //    {

        //        DatasetVersion datasetVersion = datasetManager.GetDatasetLatestVersion(id);

        //        var titles = MappingUtils.GetValuesFromMetadata(Convert.ToInt64(Key.Title), LinkElementType.Key,
        //                datasetVersion.Dataset.MetadataStructure.Id, XmlUtility.ToXDocument(datasetVersion.Metadata));

        //        var authors = MappingUtils.GetValuesFromMetadata(Convert.ToInt64(Key.Author), LinkElementType.Key,
        //                datasetVersion.Dataset.MetadataStructure.Id, XmlUtility.ToXDocument(datasetVersion.Metadata));

        //        var barcodes = MappingUtils.GetValuesFromMetadata(Convert.ToInt64(Key.Barcode), LinkElementType.Key,
        //                datasetVersion.Dataset.MetadataStructure.Id, XmlUtility.ToXDocument(datasetVersion.Metadata));

        //        var matchingObj = MappingUtils.GetValuesWithParentTypeFromMetadata(Convert.ToInt64(Key.Barcode), LinkElementType.Key,
        //                datasetVersion.Dataset.MetadataStructure.Id, XmlUtility.ToXDocument(datasetVersion.Metadata));


        //        var plots = MappingUtils.GetValuesFromMetadata(Convert.ToInt64(Key.Plot), LinkElementType.Key,
        //                datasetVersion.Dataset.MetadataStructure.Id, XmlUtility.ToXDocument(datasetVersion.Metadata));


        //        var sites = MappingUtils.GetValuesFromMetadata(Convert.ToInt64(Key.Site), LinkElementType.Key,
        //                datasetVersion.Dataset.MetadataStructure.Id, XmlUtility.ToXDocument(datasetVersion.Metadata));

        //        //MappingUtils.GetAllMatchesInSystem()

        //        string firstTitle = titles.FirstOrDefault();
        //        string firstAuthor = authors.FirstOrDefault();
        //        string plot = plots.FirstOrDefault();
        //        string site = sites.FirstOrDefault();

        //        IBarcodeWriter writer = new BarcodeWriter
        //        {
        //            Format = BarcodeFormat.QR_CODE
        //        };

        //        string url = Path.Combine(link, id.ToString());

        //        var result = writer.Write(url);
        //        var barcodeBitmap = new Bitmap(result);
        //        Image qrcodeImage;
        //        qrcodeImage = barcodeBitmap;


        //        foreach (var bc in matchingObj)
        //        {
        //            string filepath;
        //            Image barcodeImage;

        //            string barcodeName = "";
        //            string type = bc.Parent;

        //            int l = bc.Value.ToString().Length;
        //            if (l < 8)
        //            {
        //                int x = 8 - l;
        //                for (int i = 0; i < x; i++)
        //                {
        //                    barcodeName += "0";
        //                }

        //            }
        //            barcodeName += bc.Value;

        //            barcodeImage = Code128Rendering.MakeBarcodeImage(barcodeName, 2, false);



        //            BarCodeLabelModel child = new BarCodeLabelModel();
        //            child.BarCode = barcodeImage;
        //            child.QRCode = qrcodeImage;
        //            child.Id = id;
        //            child.BarCodeText = barcodeName;
        //            child.Owner = firstAuthor;
        //            child.Input = firstTitle;
        //            child.Plot = plot;
        //            child.Site = site;
        //            child.Date = DateTime.Now.ToString();
        //            child.Type = type;



        //            model.Add(child);
        //        }

        //        return PartialView("BarcodeGeneratorView", model);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //    finally
        //    {
        //        datasetManager.Dispose();
        //    }
        //}

        private XmlDocument checkForUpdates(XmlDocument metadata, long mdsId)
        {
            // get the current xml

            XmlDocument currentXmlDocument = metadata;


            // create a new xml
            XmlDocument newXmlDocument = null;

            if (mdsId > 0)
            {
                var xmlMetadatWriter = new XmlMetadataWriter(XmlNodeMode.xPath);

                newXmlDocument = XmlUtility.ToXmlDocument(xmlMetadatWriter.CreateMetadataXml(mdsId));

            }
            // compare

            if (newXmlDocument != null && currentXmlDocument != null)
            {
                if (newXmlDocument.DocumentElement != null)
                {
                    currentXmlDocument = compare(newXmlDocument.DocumentElement, currentXmlDocument);
                }
            }

            // set values


            string stemSlice = "StemSlice";
            string branch = "Branch";
            string brak = "Brak";

            XmlNode type = currentXmlDocument.GetElementsByTagName("TypeType")[0];
            type.InnerText = "Tree";

            XmlNode sampleType = currentXmlDocument.GetElementsByTagName("SampleTypeType")[0];

            string sampleTypeValue = "";

            if (currentXmlDocument.GetElementsByTagName("StemSlice") != null) sampleTypeValue = "StemSlice";
            else if (currentXmlDocument.GetElementsByTagName("Branch") != null) sampleTypeValue = "Branch";
            else if (currentXmlDocument.GetElementsByTagName("Bark") != null) sampleTypeValue = "Bark";


            sampleType.InnerText = sampleTypeValue;

            // store in taskmanager
            return currentXmlDocument;
        }

        private XmlDocument compare(XmlNode node, XmlDocument currentXmlDocument)
        {

            if (node.HasChildNodes)
            {
                XmlNode prev = null;

                foreach (XmlNode child in node.ChildNodes)
                {

                    string xpath = XmlUtility.GetDirectXPathToNode(child);
                    if (currentXmlDocument.SelectSingleNode(xpath) == null)
                    {
                        if (prev == null)
                        {
                            string parentXPath = XmlUtility.GetDirectXPathToNode(node);
                            var parent = currentXmlDocument.SelectSingleNode(parentXPath);
                            parent.AppendChild(currentXmlDocument.ImportNode(child, true));
                        }
                        else
                        {
                            string parentXPath = XmlUtility.GetDirectXPathToNode(node);
                            var parent = currentXmlDocument.SelectSingleNode(parentXPath);
                            string prevXpath = XmlUtility.GetDirectXPathToNode(prev);
                            var prevNode = currentXmlDocument.SelectSingleNode(prevXpath);
                            parent.InsertAfter(currentXmlDocument.ImportNode(child, true), prevNode);

                        }
                    }
                    else
                    {
                        currentXmlDocument = compare(child, currentXmlDocument);
                    }

                    prev = child;
                }
            }

            return currentXmlDocument;
        }

    }
}