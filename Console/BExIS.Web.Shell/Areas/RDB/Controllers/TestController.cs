using BExIS.Dlm.Entities.Data;
using BExIS.Dlm.Entities.MetadataStructure;
using BExIS.Dlm.Services.Data;
using BExIS.Rdb.Services;
using BExIS.Xml.Helpers;
using BExIS.Xml.Helpers.Mapping;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Vaiona.Persistence.Api;
using Vaiona.Utils.Cfg;

namespace BExIS.Modules.Rdb.UI.Controllers
{
    public class TestController : Controller
    {
        // GET: Test
        public ActionResult Index()
        {
            BarcodeManager bm = new BarcodeManager();

            long nextBarcodeId = bm.GetNextBarcodeId();
            nextBarcodeId = bm.GetNextBarcodeId();
            nextBarcodeId = bm.GetNextBarcodeId();
            nextBarcodeId = bm.GetNextBarcodeId();
            nextBarcodeId = bm.GetNextBarcodeId();

            return View();
        }

        public ActionResult Refactor(long id)
        {
            XmlDatasetHelper xmlDatasetHelper = new XmlDatasetHelper();

            try
            {
                using (var datasetManager = new DatasetManager())
                {
                    
                    
                    var datasetVersion = datasetManager.GetDatasetLatestVersion(id);
                    var metadata = datasetVersion.Metadata;

                    // metadataStructure ID
                    var metadataStructureId = datasetVersion.Dataset.MetadataStructure.Id;
                    var metadataStructrueName = this.GetUnitOfWork().GetReadOnlyRepository<MetadataStructure>().Get(metadataStructureId).Name;

                    // loadMapping file
                    var path_mappingFile = Path.Combine(AppConfiguration.GetModuleWorkspacePath("DIM"), XmlMetadataImportHelper.GetMappingFileName(metadataStructureId, TransmissionType.mappingFileTransform, "oldToNew"));

                    // XML mapper + mapping file
                    var xmlMapperManager = new XmlMapperManager(TransactionDirection.InternToIntern);
                    xmlMapperManager.Load(path_mappingFile, "IDIV");

                    // generate intern metadata without internal attributes
                    var metadataResult = xmlMapperManager.Generate(metadata, 1, true);

                    // generate intern template metadata xml with needed attribtes
                    var xmlMetadatWriter = new XmlMetadataWriter(BExIS.Xml.Helpers.XmlNodeMode.xPath);

                    var metadataXml = xmlMetadatWriter.CreateMetadataXml(metadataStructureId,
                        XmlUtility.ToXDocument(metadataResult));

                    var metadataXmlTemplate = XmlMetadataWriter.ToXmlDocument(metadataXml);

                    // set attributes FROM metadataXmlTemplate TO metadataResult
                    var completeMetadata = XmlMetadataImportHelper.FillInXmlValues(metadataResult,
                        metadataXmlTemplate);

                    if (completeMetadata != null)
                    {
                        string title = "";
                        if (datasetManager.IsDatasetCheckedOutFor(id, "David") || datasetManager.CheckOutDataset(id, "David"))
                        {
                            DatasetVersion workingCopy = datasetManager.GetDatasetWorkingCopy(id);
                            workingCopy.Metadata = completeMetadata;
                            workingCopy.Title = xmlDatasetHelper.GetInformation(id, completeMetadata, NameAttributeValues.title);
                            workingCopy.Description = xmlDatasetHelper.GetInformation(id, completeMetadata, NameAttributeValues.description);

                            //check if modul exist
                            int v = 1;
                            if (workingCopy.Dataset.Versions != null && workingCopy.Dataset.Versions.Count > 1) v = workingCopy.Dataset.Versions.Count();

                            //set status
                            if (workingCopy.StateInfo == null) workingCopy.StateInfo = new Vaiona.Entities.Common.EntityStateInfo();
                            workingCopy.StateInfo.State = DatasetStateInfo.NotValid.ToString();

                            title = workingCopy.Title;
                            if (string.IsNullOrEmpty(title)) title = "No Title available.";

                            datasetManager.EditDatasetVersion(workingCopy, null, null, null);
                            datasetManager.CheckInDataset(id, "refactor because of schema changes", "David", ViewCreationBehavior.None);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return View("Index");
        }

        public ActionResult RefactorAll()
        {
            XmlDatasetHelper xmlDatasetHelper = new XmlDatasetHelper();


            using (var datasetManager = new DatasetManager())
            {
                var datasetids = datasetManager.DatasetRepo.Query().Where(d => d.MetadataStructure.Id.Equals(15)).Select(d=>d.Id);
                var datasetVersions = datasetManager.GetDatasetLatestMetadataVersions();

                foreach (var id in datasetids)
                {
                    var datasetversion = datasetManager.GetDatasetLatestVersion(id);
                    var metadata = datasetversion.Metadata;
                    var metadataStructureId = datasetversion.Dataset.MetadataStructure.Id;
                    // metadataStructure ID

                    var metadataStructrueName = this.GetUnitOfWork().GetReadOnlyRepository<MetadataStructure>().Get(metadataStructureId).Name;

                    // loadMapping file
                    var path_mappingFile = Path.Combine(AppConfiguration.GetModuleWorkspacePath("DIM"), XmlMetadataImportHelper.GetMappingFileName(metadataStructureId, TransmissionType.mappingFileTransform, "oldToNew"));

                    // XML mapper + mapping file
                    var xmlMapperManager = new XmlMapperManager(TransactionDirection.InternToIntern);
                    xmlMapperManager.Load(path_mappingFile, "IDIV");

                    // generate intern metadata without internal attributes
                    var metadataResult = xmlMapperManager.Generate(metadata, 1, true);

                    // generate intern template metadata xml with needed attribtes
                    var xmlMetadatWriter = new XmlMetadataWriter(BExIS.Xml.Helpers.XmlNodeMode.xPath);

                    var metadataXml = xmlMetadatWriter.CreateMetadataXml(metadataStructureId,
                        XmlUtility.ToXDocument(metadataResult));

                    var metadataXmlTemplate = XmlMetadataWriter.ToXmlDocument(metadataXml);

                    // set attributes FROM metadataXmlTemplate TO metadataResult
                    var completeMetadata = XmlMetadataImportHelper.FillInXmlValues(metadataResult,
                        metadataXmlTemplate);

                    if (completeMetadata != null)
                    {
                        string title = "";
                        if (datasetManager.IsDatasetCheckedOutFor(id, "David") || datasetManager.CheckOutDataset(id, "David"))
                        {
                            DatasetVersion workingCopy = datasetManager.GetDatasetWorkingCopy(id);
                            workingCopy.Metadata = completeMetadata;
                            workingCopy.Title = xmlDatasetHelper.GetInformation(id, completeMetadata, NameAttributeValues.title);
                            workingCopy.Description = xmlDatasetHelper.GetInformation(id, completeMetadata, NameAttributeValues.description);

                            //check if modul exist
                            int v = 1;
                            if (workingCopy.Dataset.Versions != null && workingCopy.Dataset.Versions.Count > 1) v = workingCopy.Dataset.Versions.Count();

                            //set status
                            if (workingCopy.StateInfo == null) workingCopy.StateInfo = new Vaiona.Entities.Common.EntityStateInfo();
                            workingCopy.StateInfo.State = DatasetStateInfo.NotValid.ToString();

                            title = workingCopy.Title;
                            if (string.IsNullOrEmpty(title)) title = "No Title available.";

                            datasetManager.EditDatasetVersion(workingCopy, null, null, null);
                            datasetManager.CheckInDataset(id, "refactor because of schema changes", "David", ViewCreationBehavior.None);
                        }
                    }
                }
            }

            return View("Index");
        }
    }
}