using BExIS.Dcm.CreateDatasetWizard;
using BExIS.Dcm.UploadWizard;
using BExIS.Dcm.Wizard;
using BExIS.Ddm.Api;
using BExIS.Dim.Entities.Mapping;
using BExIS.Dim.Helpers.Mapping;
using BExIS.Dlm.Entities.Administration;
using BExIS.Dlm.Entities.Data;
using BExIS.Dlm.Entities.DataStructure;
using BExIS.Dlm.Entities.MetadataStructure;
using BExIS.Dlm.Entities.Party;
using BExIS.Dlm.Services.Administration;
using BExIS.Dlm.Services.Data;
using BExIS.Dlm.Services.DataStructure;
using BExIS.Dlm.Services.MetadataStructure;
using BExIS.Dlm.Services.Party;
using BExIS.Modules.Dcm.UI.Helpers;
using BExIS.Modules.Rdb.UI.Models;
using BExIS.Security.Entities.Authorization;
using BExIS.Security.Entities.Objects;
using BExIS.Security.Entities.Subjects;
using BExIS.Security.Services.Authorization;
using BExIS.Security.Services.Objects;
using BExIS.Security.Services.Subjects;
using BExIS.Security.Services.Utilities;
using BExIS.Utils.Data.Upload;
using BExIS.Utils.Extensions;
using BExIS.Web.Shell.Helpers;
using BExIS.Web.Shell.Models;
using BExIS.Xml.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml;
using System.Xml.Linq;
using Vaiona.Entities.Common;
using Vaiona.IoC;
using Vaiona.Logging;
using Vaiona.Persistence.Api;
using Vaiona.Web.Extensions;
using Vaiona.Web.Mvc.Models;
using Vaiona.Web.Mvc.Modularity;

namespace BExIS.Modules.Rdb.UI.Controllers
{
    public class CreateSampleController : Controller
    {
        private CreateTaskmanager TaskManager;
        private XmlDatasetHelper xmlDatasetHelper = new XmlDatasetHelper();

        #region Create a Sample Setup Page

        //
        // GET: /DCM/CreateDataset/
        /// <summary>
        /// Load the createDataset action with different parameter type options
        /// type eg ("DataStructureId", "DatasetId", "MetadataStructureId")
        /// </summary>
        /// <param name="id"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public ActionResult Index(long id = -1, string type = "")
        {
            ViewBag.Title = PresentationModel.GetViewTitleForTenant("Create Sample", this.Session.GetTenant());

            Session["CreateDatasetTaskmanager"] = null;
            if (TaskManager == null) TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];

            if (TaskManager == null)
            {

                TaskManager = new CreateTaskmanager();

                TaskManager.AddToBus(CreateTaskmanager.ENTITY_NAME, "Sample");
                TaskManager.AddToBus(CreateTaskmanager.ENTITY_TYPE_NAME, typeof(Dataset));

                Session["CreateDatasetTaskmanager"] = TaskManager;
                Session["MetadataStructureViewList"] = LoadMetadataStructureViewList();
                Session["DatasetViewList"] = LoadDatasetViewList();


                setAdditionalFunctions();

                //set Entity to TaskManager
                TaskManager.AddToBus(CreateTaskmanager.ENTITY_CLASS_PATH, "BExIS.Rdb.Entities.Sample");

                SetupModel Model = GetDefaultModel();

                //if id is set and its type dataset
                if (id != -1 && type.ToLower().Equals("datasetid"))
                {
                    ViewBag.Title = PresentationModel.GetViewTitleForTenant("Copy Sample", this.Session.GetTenant());

                    using (DatasetManager datasetManager = new DatasetManager())
                    {
                        Dataset dataset = datasetManager.DatasetRepo.Get(id);
                        Model.SelectedDatasetId = id;
                        Model.SelectedMetadataStructureId = dataset.MetadataStructure.Id;
                        Model.SelectedDataStructureId = dataset.DataStructure.Id;
                        Model.BlockMetadataStructureId = true;
                        Model.BlockDatasetId = true;
                    }
                }

                if (id != -1 && type.ToLower().Equals("metadatastructureid"))
                {
                    ViewBag.Title = PresentationModel.GetViewTitleForTenant("Copy Sample", this.Session.GetTenant());
                    Model.SelectedMetadataStructureId = id;
                }

                if (id != -1 && type.ToLower().Equals("datastructureid"))
                {
                    ViewBag.Title = PresentationModel.GetViewTitleForTenant("Copy Sample", this.Session.GetTenant());
                    Model.SelectedDataStructureId = id;
                    if (TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATASTRUCTURE_ID))
                        Model.SelectedMetadataStructureId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.METADATASTRUCTURE_ID]);
                }


                return View(Model);
            }

            return View();
        }

        /// <summary>
        /// ReLoad the createDataset action with different parameter type options
        /// type eg ("DataStructureId", "DatasetId", "MetadataStructureId")
        /// </summary>
        /// <param name="id"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public ActionResult ReloadIndex(long id = -1, string type = "")
        {
            ViewBag.Title = PresentationModel.GetViewTitleForTenant("...", this.Session.GetTenant());

            if (TaskManager == null) TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];

            using (DatasetManager datasetManager = new DatasetManager())
            {
                SetupModel Model = GetDefaultModel();

                //if id is set and its type dataset
                if (id != -1 && type.ToLower().Equals("datasetid"))
                {
                    Dataset dataset = datasetManager.DatasetRepo.Get(id);
                    Model.SelectedDatasetId = id;
                    Model.SelectedMetadataStructureId = dataset.MetadataStructure.Id;
                    Model.SelectedDataStructureId = dataset.DataStructure.Id;
                    Model.BlockMetadataStructureId = true;
                    Model.BlockDatasetId = false;

                    TaskManager.AddToBus(CreateTaskmanager.COPY_OF_ENTITY_ID, id);

                }

                if (id != -1 && type.ToLower().Equals("metadatastructureid"))
                {
                    TaskManager.AddToBus(CreateTaskmanager.METADATASTRUCTURE_ID, id);
                    Model.SelectedMetadataStructureId = id;

                    if (TaskManager.Bus.ContainsKey(CreateTaskmanager.DATASTRUCTURE_ID))
                        Model.SelectedDataStructureId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.DATASTRUCTURE_ID]);

                    if (TaskManager.Bus.ContainsKey(CreateTaskmanager.COPY_OF_ENTITY_ID))
                    {
                        Model.SelectedDatasetId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.COPY_OF_ENTITY_ID]);
                        Dataset dataset = datasetManager.DatasetRepo.Get(Model.SelectedDatasetId);
                        Model.BlockMetadataStructureId = true;
                        Model.BlockDatasetId = false;
                    }

                }

                if (id != -1 && type.ToLower().Equals("datastructureid"))
                {
                    TaskManager.AddToBus(CreateTaskmanager.DATASTRUCTURE_ID, id);
                    Model.SelectedDataStructureId = id;

                    if (TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATASTRUCTURE_ID))
                        Model.SelectedMetadataStructureId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.METADATASTRUCTURE_ID]);

                    if (TaskManager.Bus.ContainsKey(CreateTaskmanager.COPY_OF_ENTITY_ID))
                    {
                        Model.SelectedDatasetId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.COPY_OF_ENTITY_ID]);

                        Dataset dataset = datasetManager.DatasetRepo.Get(Model.SelectedDatasetId);
                        Model.BlockMetadataStructureId = true;
                        Model.BlockDatasetId = false;
                    }
                }

                return View("Index", Model);
            }
        }

        public ActionResult StoreSelectedDatasetSetup(SetupModel model)
        {
            CreateTaskmanager TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];
            using (DatasetManager dm = new DatasetManager())
            {

                if (model == null)
                {
                    model = GetDefaultModel();
                    return PartialView("Index", model);
                }

                model = LoadLists(model);

                if (ModelState.IsValid)
                {
                    TaskManager.AddToBus(CreateTaskmanager.METADATASTRUCTURE_ID, model.SelectedMetadataStructureId);
                    TaskManager.AddToBus(CreateTaskmanager.DATASTRUCTURE_ID, model.SelectedDataStructureId);

                    // set datastructuretype
                    TaskManager.AddToBus(CreateTaskmanager.DATASTRUCTURE_TYPE, GetDataStructureType(model.SelectedDataStructureId));

                    //dataset is selected
                    if (model.SelectedDatasetId != 0 && model.SelectedDatasetId != -1)
                    {
                        if (dm.IsDatasetCheckedIn(model.SelectedDatasetId))
                        {
                            DatasetVersion datasetVersion = dm.GetDatasetLatestVersion(model.SelectedDatasetId);
                            TaskManager.AddToBus(CreateTaskmanager.RESEARCHPLAN_ID,
                                datasetVersion.Dataset.ResearchPlan.Id);
                            TaskManager.AddToBus(CreateTaskmanager.ENTITY_TITLE,
                                xmlDatasetHelper.GetInformation(datasetVersion.Dataset.Id, NameAttributeValues.title));

                            // set datastructuretype
                            TaskManager.AddToBus(CreateTaskmanager.DATASTRUCTURE_TYPE,
                                GetDataStructureType(model.SelectedDataStructureId));

                            // set MetadataXml From selected existing Dataset
                            XDocument metadata = XmlUtility.ToXDocument(datasetVersion.Metadata);
                            SetXml(metadata);
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Sample is just in processing");
                        }
                    }
                    else
                    {
                        ResearchPlanManager rpm = new ResearchPlanManager();
                        TaskManager.AddToBus(CreateTaskmanager.RESEARCHPLAN_ID, rpm.Repo.Get().First().Id);
                    }


                    return RedirectToAction("StartMetadataEditor", "Form", new { area = "DCM" });

                }

                return View("Index", model);
            }
        }

        [HttpPost]
        public ActionResult StoreSelectedDataset(long id)
        {
            if (TaskManager == null) TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];

            using (DatasetManager dm = new DatasetManager())
            {
                Dataset dataset = dm.GetDataset(id);

                SetupModel Model = GetDefaultModel();

                if (id == -1)
                {

                    if (TaskManager.Bus.ContainsKey(CreateTaskmanager.DATASTRUCTURE_ID))
                        Model.SelectedDataStructureId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.DATASTRUCTURE_ID]);

                    if (TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATASTRUCTURE_ID))
                        Model.SelectedMetadataStructureId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.METADATASTRUCTURE_ID]);

                }
                else
                {
                    Model.SelectedDatasetId = id;
                    Model.SelectedDataStructureId = dataset.DataStructure.Id;
                    Model.SelectedMetadataStructureId = dataset.MetadataStructure.Id;

                    Model.BlockMetadataStructureId = true;

                    //add to Bus
                    TaskManager.AddToBus(CreateTaskmanager.DATASTRUCTURE_ID, dataset.DataStructure.Id);
                    TaskManager.AddToBus(CreateTaskmanager.METADATASTRUCTURE_ID, dataset.MetadataStructure.Id);
                    TaskManager.AddToBus(CreateTaskmanager.COPY_OF_ENTITY_ID, dataset.Id);
                }

                return PartialView("Index", Model);
            }
        }

        [HttpPost]
        public ActionResult StoreSelectedOption(long id, string type)
        {
            TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];
            string key = "";

            switch (type)
            {
                case "ms": key = CreateTaskmanager.METADATASTRUCTURE_ID; break;
                case "ds": key = CreateTaskmanager.DATASTRUCTURE_ID; break;
            }

            if (key != "")
            {
                if (TaskManager.Bus.ContainsKey(key))
                    TaskManager.Bus[key] = id;
                else
                    TaskManager.Bus.Add(key, id);
            }

            return Content("");
        }

        #region setup parameter selection actions

        [HttpGet]
        public ActionResult ShowListOfDatasets()
        {
            List<ListViewItem> datasets = LoadDatasetViewList();

            EntitySelectorModel model = BexisModelManager.LoadEntitySelectorModel(
                datasets,
                new EntitySelectorModelAction("ShowListOfDatasetsReciever", "CreateSample", "RDB"));

            model.Title = "Select a Sample as Template";

            return PartialView("_EntitySelectorInWindowView", model);
        }

        public ActionResult ShowListOfDatasetsReciever(long id)
        {
            return RedirectToAction("ReloadIndex", "CreateSample", new { id = id, type = "Datasetid" });
        }


        public ActionResult ShowListOfDataStructuresReciever(long id)
        {
            return RedirectToAction("ReloadIndex", "CreateSample", new { id = id, type = "DataStructureId" });
        }

        [HttpGet]
        public ActionResult ShowListOfMetadataStructures()
        {
            List<ListViewItem> metadataStructures = LoadMetadataStructureViewList();

            EntitySelectorModel model = BexisModelManager.LoadEntitySelectorModel(
                 metadataStructures,
                 new EntitySelectorModelAction("ShowListOfMetadataStructuresReciever", "CreateSample", "RDB"));

            model.Title = "Select a Metadata Structure";

            return PartialView("_EntitySelectorInWindowView", model);
        }

        public ActionResult ShowListOfMetadataStructuresReciever(long id)
        {
            return RedirectToAction("ReloadIndex", "CreateSample", new { id = id, type = "MetadataStructureId" });
        }

        #endregion

        private SetupModel GetDefaultModel()
        {
            SetupModel model = new SetupModel();

            model = LoadLists(model);

            if (TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATASTRUCTURE_ID))
                model.SelectedMetadataStructureId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.METADATASTRUCTURE_ID]);

            if (TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATASTRUCTURE_ID))
                model.SelectedMetadataStructureId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.METADATASTRUCTURE_ID]);

            model.BlockDatasetId = false;
            model.BlockMetadataStructureId = false;

            return model;
        }

        /// <summary>
        /// load all existing lists for this step
        /// if there are stored in the session
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private SetupModel LoadLists(SetupModel model)
        {
            if ((List<ListViewItem>)Session["MetadataStructureViewList"] != null) model.MetadataStructureViewList = (List<ListViewItem>)Session["MetadataStructureViewList"];
            if ((List<ListViewItem>)Session["DatasetViewList"] != null) model.DatasetViewList = (List<ListViewItem>)Session["DatasetViewList"];

            return model;
        }

        #endregion

        /// <summary>
        /// Store the incoming xmldocument in the bus of the Create TaskManager with
        /// the METADATA_XML key 
        /// </summary>
        /// <param name="metadataXml"></param>
        private void SetXml(XDocument metadataXml)
        {
            TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];

            // load metadatastructure with all packages and attributes

            if (metadataXml != null)
            {
                // locat path
                //string path = Path.Combine(AppConfiguration.GetModuleWorkspacePath("DCM"), "metadataTemp.Xml");

                TaskManager.AddToBus(CreateTaskmanager.METADATA_XML, metadataXml);

                //setup loaded
                if (TaskManager.Bus.ContainsKey(CreateTaskmanager.SETUP_LOADED))
                    TaskManager.Bus[CreateTaskmanager.SETUP_LOADED] = true;
                else
                    TaskManager.Bus.Add(CreateTaskmanager.SETUP_LOADED, true);
            }

        }

        #region Submit And Create And Finish And Cancel and Reset

        public JsonResult Submit(bool valid)
        {
            try
            {
                // create and submit Dataset
                long datasetId = SubmitSample(valid);

                return Json(new { result = "redirect", url = Url.Action("Show", "Sample", new { area = "RDB", id = datasetId }) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { result = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        //public ActionResult Submit()
        //{

        //    if (TaskManager == null) TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];



        //    // create and submit Dataset 
        //    long datasetId = SubmitSample();

        //    bool editMode = false;


        //    if (TaskManager.Bus.ContainsKey(CreateTaskmanager.EDIT_MODE))
        //        editMode = (bool)TaskManager.Bus[CreateTaskmanager.EDIT_MODE];

        //    if (editMode)
        //        return RedirectToAction("LoadMetadata", "Form", new { area = "DCM", entityId = datasetId, locked = true, created = false, fromEditMode = true });
        //    else
        //        return RedirectToAction("LoadMetadata", "Form", new { area = "DCM", entityId = datasetId, locked = true, created = true });
        //}

        /// <summary>
        /// Submit a Dataset based on the imformations
        /// in the CreateTaskManager
        /// </summary>
        public long SubmitSample(bool valid)
        {
            #region create sample

            using (DatasetManager dm = new DatasetManager())
            using (DataStructureManager dsm = new DataStructureManager())
            using (MetadataStructureManager msm = new MetadataStructureManager())
            using (ResearchPlanManager rpm = new ResearchPlanManager())
            using(EntityPermissionManager entityPermissionManager = new EntityPermissionManager())
            {
                XmlDatasetHelper xmlDatasetHelper = new XmlDatasetHelper();
                string title = "";
                long datasetId = 0;
                bool newDataset = true;

                try
                {
                    TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];

                    if (TaskManager.Bus.ContainsKey(CreateTaskmanager.DATASTRUCTURE_ID)
                        && TaskManager.Bus.ContainsKey(CreateTaskmanager.RESEARCHPLAN_ID)
                        && TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATASTRUCTURE_ID))
                    {
                        // for e new dataset
                        if (!TaskManager.Bus.ContainsKey(CreateTaskmanager.ENTITY_ID))
                        {
                            long datastructureId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.DATASTRUCTURE_ID]);
                            long researchPlanId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.RESEARCHPLAN_ID]);
                            long metadataStructureId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.METADATASTRUCTURE_ID]);

                            DataStructure dataStructure = dsm.StructuredDataStructureRepo.Get(datastructureId);
                            //if datastructure is not a structured one
                            if (dataStructure == null) dataStructure = dsm.UnStructuredDataStructureRepo.Get(datastructureId);

                            //id not exist
                            if (dataStructure == null )
                            {
                                dataStructure = dsm.UnStructuredDataStructureRepo.Get().Where(d=>d.Name.ToLower().Equals("none")).FirstOrDefault();
                            }

                            ResearchPlan rp = rpm.Repo.Get(researchPlanId);
                            MetadataStructure metadataStructure = msm.Repo.Get(metadataStructureId);

                            var ds = dm.CreateEmptyDataset(dataStructure, rp, metadataStructure);
                            datasetId = ds.Id;

                            // add security
                            if (GetUsernameOrDefault() != "DEFAULT")
                            {
                                string entity_name = "Dataset";
                                Type entity_type = typeof(Dataset);
                                
                                if (TaskManager.Bus.ContainsKey(CreateTaskmanager.ENTITY_NAME))
                                {
                                    entity_name = TaskManager.Bus[CreateTaskmanager.ENTITY_NAME].ToString();
                                }

                                if (TaskManager.Bus.ContainsKey(CreateTaskmanager.ENTITY_TYPE_NAME))
                                {
                                    entity_type = (Type)TaskManager.Bus[CreateTaskmanager.ENTITY_TYPE_NAME];
                                }


                                entityPermissionManager.Create<User>(GetUsernameOrDefault(), entity_name, entity_type, ds.Id, Enum.GetValues(typeof(RightType)).Cast<RightType>().ToList());
                            }
                        }
                        else
                        {
                            datasetId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.ENTITY_ID]);
                            newDataset = false;
                        }

                        TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];

                        if (dm.IsDatasetCheckedOutFor(datasetId, GetUsernameOrDefault()) || dm.CheckOutDataset(datasetId, GetUsernameOrDefault()))
                        {
                            DatasetVersion workingCopy = dm.GetDatasetWorkingCopy(datasetId);

                            if (TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATA_XML))
                            {
                                XDocument xMetadata = (XDocument)TaskManager.Bus[CreateTaskmanager.METADATA_XML];
                                workingCopy.Metadata = Xml.Helpers.XmlWriter.ToXmlDocument(xMetadata);

                                workingCopy.Title = xmlDatasetHelper.GetInformation(datasetId, workingCopy.Metadata, NameAttributeValues.title);
                                workingCopy.Description = xmlDatasetHelper.GetInformation(datasetId, workingCopy.Metadata, NameAttributeValues.description);

                                //check if modul exist
                                int v = 1;
                                if (workingCopy.Dataset.Versions != null && workingCopy.Dataset.Versions.Count > 1) v = workingCopy.Dataset.Versions.Count();

                                TaskManager.Bus[CreateTaskmanager.METADATA_XML] = setSystemValuesToMetadata(datasetId, v, workingCopy.Dataset.MetadataStructure.Id, workingCopy.Metadata, newDataset);
                            }

                            //set status
                            workingCopy = setStateInfo(workingCopy, valid);
                            //set modifikation
                            workingCopy = setModificationInfo(workingCopy, newDataset, GetUsernameOrDefault(), "Metadata");

                            title = workingCopy.Title;
                            if (string.IsNullOrEmpty(title)) title = "No Title available.";

                            TaskManager.AddToBus(CreateTaskmanager.ENTITY_TITLE, title);//workingCopy.Metadata.SelectNodes("Metadata/Description/Description/Title/Title")[0].InnerText);
                            TaskManager.AddToBus(CreateTaskmanager.ENTITY_ID, datasetId);

                            dm.EditDatasetVersion(workingCopy, null, null, null);
                            dm.CheckInDataset(datasetId, "", GetUsernameOrDefault(), ViewCreationBehavior.None);

                            #region set releationships

                            //todo check if dim is active
                            // todo call to  a function in dim
                            setRelationships(datasetId, workingCopy.Dataset.MetadataStructure.Id, workingCopy.Metadata);

                            // references

                            #endregion set releationships

                            #region set references

                            setReferences(workingCopy);

                            #endregion set references

                            if (this.IsAccessible("DDM", "SearchIndex", "ReIndexSingle"))
                            {
                                var x = this.Run("DDM", "SearchIndex", "ReIndexSingle", new RouteValueDictionary() { { "id", datasetId } });
                            }

                            LoggerFactory.LogData(datasetId.ToString(), typeof(Dataset).Name, Vaiona.Entities.Logging.CrudState.Created);

                            if (newDataset)
                            {
                                var es = new EmailService();
                                es.Send(MessageHelper.GetCreateDatasetHeader(),
                                    MessageHelper.GetCreateDatasetMessage(datasetId, title, GetUsernameOrDefault()),
                                    ConfigurationManager.AppSettings["SystemEmail"]
                                    );
                            }
                            else
                            {
                                var es = new EmailService();
                                es.Send(MessageHelper.GetUpdateDatasetHeader(),
                                    MessageHelper.GetUpdateDatasetMessage(datasetId, title, GetUsernameOrDefault()),
                                    ConfigurationManager.AppSettings["SystemEmail"]
                                    );
                            }
                        }

                        return datasetId;
                    }
                }
                catch (Exception ex)
                {
                    var es = new EmailService();
                    es.Send(MessageHelper.GetUpdateDatasetHeader(),
                        ex.Message,
                        ConfigurationManager.AppSettings["SystemEmail"]
                        );
                }

            }
            #endregion

            return -1;
        }

        #region Options

        /// <summary>
        /// Load the UploadWizard with preselected parameters
        /// and redirect to "UploadWizard", "Submit", area = "DCM"
        /// </summary>
        /// <returns></returns>
        public ActionResult StartUploadWizard()
        {
            TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];

            DataStructureType type = new DataStructureType();

            if (TaskManager.Bus.ContainsKey(CreateTaskmanager.DATASTRUCTURE_TYPE))
            {
                type = (DataStructureType)TaskManager.Bus[CreateTaskmanager.DATASTRUCTURE_TYPE];
            }

            long datasetid = 0;
            // set parameters for upload process to pass it with the action
            if (TaskManager.Bus.ContainsKey(CreateTaskmanager.ENTITY_ID))
            {
                datasetid = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.ENTITY_ID]);
            }

            Session["CreateDatasetTaskmanager"] = null;
            TaskManager = null;

            return RedirectToAction("UploadWizard", "Submit", new { area = "DCM", type = type, datasetid = datasetid });

        }

        /// <summary>
        /// redirect to the DDM/Data/ShowData Action
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult ShowData(long id)
        {
            return RedirectToAction("ShowData", "Data", new { area = "DDM", id = id });
        }

        public ActionResult Cancel()
        {
            //public ActionResult LoadMetadata(long datasetId, bool locked = false, bool created = false, bool fromEditMode = false, bool resetTaskManager = false, XmlDocument newMetadata = null)

            TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];
            if (TaskManager != null)
            {
                using (DatasetManager dm = new DatasetManager())
                {
                    long datasetid = -1;
                    bool resetTaskManager = true;
                    XmlDocument metadata = null;
                    bool editmode = false;
                    bool created = false;

                    if (TaskManager.Bus.ContainsKey(CreateTaskmanager.ENTITY_ID))
                    {
                        datasetid = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.ENTITY_ID]);
                    }

                    if (datasetid > -1 && dm.IsDatasetCheckedIn(datasetid))
                    {
                        metadata = dm.GetDatasetLatestMetadataVersion(datasetid);
                        editmode = true;
                        created = true;
                    }

                    return RedirectToAction("LoadMetadata", "Form", new { area = "DCM", entityId = datasetid, created = created, locked = true, fromEditMode = editmode, resetTaskManager = resetTaskManager, newMetadata = metadata });
                }
            }

            return RedirectToAction("StartMetadataEditor", "Form", new { area = "DCM" });
        }

        public ActionResult Reset()
        {
            //public ActionResult LoadMetadata(long datasetId, bool locked = false, bool created = false, bool fromEditMode = false, bool resetTaskManager = false, XmlDocument newMetadata = null)

            TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];
            if (TaskManager != null)
            {
                using (DatasetManager dm = new DatasetManager())
                {
                    long datasetid = -1;
                    bool resetTaskManager = true;
                    XmlDocument metadata = null;
                    bool editmode = false;
                    bool created = false;

                    if (TaskManager.Bus.ContainsKey(CreateTaskmanager.ENTITY_ID))
                    {
                        datasetid = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.ENTITY_ID]);
                    }

                    if (datasetid > -1 && dm.IsDatasetCheckedIn(datasetid))
                    {
                        metadata = dm.GetDatasetLatestMetadataVersion(datasetid);
                        editmode = true;
                        created = true;
                    }



                    return RedirectToAction("LoadMetadata", "Form", new { area = "DCM", entityId = datasetid, locked = false, created = created, fromEditMode = editmode, resetTaskManager = resetTaskManager, newMetadata = metadata });
                }
            }

            return RedirectToAction("StartMetadataEditor", "Form", new { area = "DCM" });
        }

        public ActionResult Copy()
        {
            TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];
            if (TaskManager != null)
            {
                if (TaskManager.Bus.ContainsKey(CreateTaskmanager.ENTITY_ID))
                {
                    long datasetid = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.ENTITY_ID]);

                    return RedirectToAction("Index", "CreateSample", new { area = "RDB", id = datasetid, type = "DatasetId" });

                }
            }
            //Index(long id = -1, string type = "")
            return RedirectToAction("Index", "CreateSample", new { area = "RDB", id = -1, type = "DatasetId" });
        }

        #endregion

        #endregion

        #region Helper

        // chekc if user exist
        // if true return usernamem otherwise "DEFAULT"
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

        private DataStructureType GetDataStructureType(long id)
        {
            using (DataStructureManager dataStructuremanager = new DataStructureManager())
            {
                DataStructure dataStructure = dataStructuremanager.AllTypesDataStructureRepo.Get(id);

                if (dataStructure is StructuredDataStructure)
                {
                    return DataStructureType.Structured;
                }

                if (dataStructure is UnStructuredDataStructure)
                {
                    return DataStructureType.Unstructured;
                }

                return DataStructureType.Structured;
            }
        }

        public List<ListViewItem> LoadMetadataStructureViewList()
        {
            using (MetadataStructureManager msm = new MetadataStructureManager())
            {
                List<ListViewItem> temp = new List<ListViewItem>();

                foreach (MetadataStructure metadataStructure in msm.Repo.Get())
                {
                    if (xmlDatasetHelper.IsActive(metadataStructure.Id) &&
                        xmlDatasetHelper.HasEntity(metadataStructure.Id, "Sample"))
                    {
                        string title = metadataStructure.Name;

                        temp.Add(new ListViewItem(metadataStructure.Id, title, metadataStructure.Description));
                    }
                }

                return temp.OrderBy(p => p.Title).ToList();
            }
        }


        public List<ListViewItem> LoadDatasetViewList()
        {
            List<ListViewItem> temp = new List<ListViewItem>();

            using (DatasetManager datasetManager = new DatasetManager())
            using (EntityPermissionManager entityPermissionManager = new EntityPermissionManager())
            //get all datasetsid where the current userer has access to
            using (UserManager userManager = new UserManager())
            {
                XmlDatasetHelper xmlDatasetHelper = new XmlDatasetHelper();

                List<long> datasetIds = entityPermissionManager.GetKeys(GetUsernameOrDefault(), "Sample",
                    typeof(Dataset), RightType.Write);

                foreach (long id in datasetIds)
                {
                    if (datasetManager.IsDatasetCheckedIn(id))
                    {
                        string title = xmlDatasetHelper.GetInformation(id, NameAttributeValues.title);
                        string description = xmlDatasetHelper.GetInformation(id, NameAttributeValues.description);

                        temp.Add(new ListViewItem(id, title, description));
                    }
                }


                return temp.OrderBy(p => p.Title).ToList();
            }
        }

        //toDo this function to DIM or BAM ??
        /// <summary>
        /// this function is parsing the xmldocument to
        /// create releationships based on releationshiptypes between datasets and person parties
        /// </summary>
        /// <param name="datasetid"></param>
        /// <param name="metadataStructureId"></param>
        /// <param name="metadata"></param>
        private void setRelationships(long datasetid, long metadataStructureId, XmlDocument metadata)
        {
            using (PartyManager partyManager = new PartyManager())
            using (PartyTypeManager partyTypeManager = new PartyTypeManager())
            using (PartyRelationshipTypeManager partyRelationshipTypeManager = new PartyRelationshipTypeManager())
            {
                try
                {
                    using (var uow = this.GetUnitOfWork())
                    {
                        //check if mappings exist between system/relationships and the metadatastructure/attr
                        // get all party mapped nodes
                        IEnumerable<XElement> complexElements = XmlUtility.GetXElementsByAttribute("partyid", XmlUtility.ToXDocument(metadata));

                        // get releaionship type id for owner
                        var relationshipTypes = uow.GetReadOnlyRepository<PartyRelationshipType>().Get().Where(
                            p => p.AssociatedPairs.Any(
                                ap => ap.SourcePartyType.Title.ToLower().Equals("dataset") || ap.TargetPartyType.Title.ToLower().Equals("dataset")
                                ));

                        #region delete relationships

                        foreach (var relationshipType in relationshipTypes)
                        {
                            bool exist = false;
                            var partyTpePair = relationshipType.AssociatedPairs.FirstOrDefault();

                            if (partyTpePair.SourcePartyType.Title.ToLower().Equals("dataset"))
                            {
                                IEnumerable<PartyRelationship> relationships = uow.GetReadOnlyRepository<PartyRelationship>().Get().Where(
                                        r =>
                                        r.SourceParty != null && r.SourceParty.Name.Equals(datasetid.ToString()) &&
                                        r.PartyTypePair != null && r.PartyTypePair.Id.Equals(partyTpePair.Id)
                                    );

                                IEnumerable<long> partyids = complexElements.Select(i => Convert.ToInt64(i.Attribute("partyid").Value));

                                foreach (PartyRelationship pr in relationships)
                                {
                                    if (!partyids.Contains(pr.TargetParty.Id)) partyManager.RemovePartyRelationship(pr);
                                }
                            }
                            else
                            {
                                IEnumerable<PartyRelationship> relationships = uow.GetReadOnlyRepository<PartyRelationship>().Get().Where(
                                        r =>
                                        r.TargetParty != null && r.TargetParty.Name.Equals(datasetid.ToString()) &&
                                        r.PartyTypePair != null && r.PartyTypePair.Id.Equals(partyTpePair.Id)
                                    );

                                IEnumerable<long> partyids = complexElements.Select(i => Convert.ToInt64(i.Attribute("partyid").Value));

                                foreach (PartyRelationship pr in relationships)
                                {
                                    if (!partyids.Contains(pr.SourceParty.Id)) partyManager.RemovePartyRelationship(pr);
                                }
                            }
                        }

                        #endregion delete relationships

                        #region add relationship

                        foreach (XElement item in complexElements)
                        {
                            if (item.HasAttributes)
                            {
                                long sourceId = Convert.ToInt64(item.Attribute("roleId").Value);
                                long id = Convert.ToInt64(item.Attribute("id").Value);
                                string type = item.Attribute("type").Value;
                                long partyid = Convert.ToInt64(item.Attribute("partyid").Value);

                                LinkElementType sourceType = LinkElementType.MetadataNestedAttributeUsage;
                                if (type.Equals("MetadataPackageUsage")) sourceType = LinkElementType.MetadataPackageUsage;

                                foreach (var relationship in relationshipTypes)
                                {
                                    // when mapping in both directions are exist
                                    if ((MappingUtils.ExistMappings(id, sourceType, relationship.Id, LinkElementType.PartyRelationshipType) &&
                                        MappingUtils.ExistMappings(relationship.Id, LinkElementType.PartyRelationshipType, id, sourceType)) ||
                                        (MappingUtils.ExistMappings(sourceId, LinkElementType.MetadataAttributeUsage, relationship.Id, LinkElementType.PartyRelationshipType) &&
                                        MappingUtils.ExistMappings(relationship.Id, LinkElementType.PartyRelationshipType, sourceId, LinkElementType.MetadataAttributeUsage)) ||
                                        (MappingUtils.ExistMappings(sourceId, LinkElementType.ComplexMetadataAttribute, relationship.Id, LinkElementType.PartyRelationshipType) &&
                                        MappingUtils.ExistMappings(relationship.Id, LinkElementType.PartyRelationshipType, sourceId, LinkElementType.ComplexMetadataAttribute)) ||
                                        (MappingUtils.ExistMappings(sourceId, LinkElementType.MetadataNestedAttributeUsage, relationship.Id, LinkElementType.PartyRelationshipType) &&
                                        MappingUtils.ExistMappings(relationship.Id, LinkElementType.PartyRelationshipType, sourceId, LinkElementType.MetadataNestedAttributeUsage)))
                                    {
                                        // create releationship

                                        // create a Party for the dataset
                                        var customAttributes = new Dictionary<String, String>();
                                        customAttributes.Add("Name", datasetid.ToString());
                                        customAttributes.Add("Id", datasetid.ToString());

                                        // get or create datasetParty
                                        Party datasetParty = partyManager.GetPartyByCustomAttributeValues(partyTypeManager.PartyTypeRepository.Get(cc => cc.Title == "Dataset").First(), customAttributes).FirstOrDefault();
                                        if (datasetParty == null) datasetParty = partyManager.Create(partyTypeManager.PartyTypeRepository.Get(cc => cc.Title == "Dataset").First(), "[description]", null, null, customAttributes);

                                        // Get user party
                                        var person = partyManager.GetParty(partyid);

                                        var partyTpePair = relationship.AssociatedPairs.FirstOrDefault();

                                        if (partyTpePair != null && person != null && datasetParty != null)
                                        {
                                            if (!uow.GetReadOnlyRepository<PartyRelationship>().Get().Any(
                                                r =>
                                                r.SourceParty != null && r.SourceParty.Id.Equals(person.Id) &&
                                                r.PartyTypePair != null && r.PartyTypePair.Id.Equals(partyTpePair.Id) &&
                                                r.TargetParty.Id.Equals(datasetid)
                                            ))
                                            {
                                                partyManager.AddPartyRelationship(
                                                    person.Id,
                                                    datasetParty.Id,
                                                    relationship.Title,
                                                    "",
                                                    partyTpePair.Id

                                                    );
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        #endregion add relationship
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

            }
        }

        private XDocument setSystemValuesToMetadata(long datasetid, long version, long metadataStructureId, XmlDocument metadata, bool newDataset)
        {
            Helpers.SystemMetadataHelper SystemMetadataHelper = new Helpers.SystemMetadataHelper();

            Key[] myObjArray = { };

            if (newDataset) myObjArray = new Key[] { Key.Id, Key.Version, Key.DateOfVersion, Key.MetadataCreationDate, Key.MetadataLastModfied };
            else myObjArray = new Key[] { Key.Id, Key.Version, Key.DateOfVersion, Key.MetadataLastModfied };

            metadata = SystemMetadataHelper.SetSystemValuesToMetadata(datasetid, version, metadataStructureId, metadata, myObjArray);

            return XmlUtility.ToXDocument(metadata);
        }

        private void setReferences(DatasetVersion datasetVersion)
        {
            EntityReferenceManager entityReferenceManager = new EntityReferenceManager();
            XmlDatasetHelper xmlDatasetHelper = new XmlDatasetHelper();
            EntityManager entityManager = new EntityManager();
            EntityReferenceHelper helper = new EntityReferenceHelper();

            try
            {
                if (datasetVersion != null)
                {
                    List<EntityReference> refs = getAllMetadataReferences(datasetVersion);

                    foreach (var singleRef in refs)
                    {
                        if (!entityReferenceManager.Exist(singleRef, true, true))
                            entityReferenceManager.Create(singleRef);
                    }
                }
            }
            finally
            {
            }
        }

        private List<EntityReference> getAllMetadataReferences(DatasetVersion datasetVersion)
        {
            List<EntityReference> tmp = new List<EntityReference>();
            EntityReferenceHelper helper = new EntityReferenceHelper();
            MappingUtils mappingUtils = new MappingUtils();
            XmlDatasetHelper xmlDatasetHelper = new XmlDatasetHelper();

            using (MetadataStructureManager metadataStructureManager = new MetadataStructureManager())
            using (DatasetManager datasetManager = new DatasetManager())
            using (EntityManager entityManager = new EntityManager())
            {
                long id = 0;
                long typeid = 0;
                int version = 0;


                if (datasetVersion != null)
                {
                    long metadataStrutcureId = datasetVersion.Dataset.MetadataStructure.Id;

                    //get entity type like dataset or sample
                    string entityName = xmlDatasetHelper.GetEntityNameFromMetadatStructure(metadataStrutcureId, new Dlm.Services.MetadataStructure.MetadataStructureManager());
                    Entity entityType = entityManager.Entities.Where(e => e.Name.Equals(entityName)).FirstOrDefault();

                    //get id of the entity type
                    id = datasetVersion.Dataset.Id;
                    typeid = entityType.Id;
                    version = datasetVersion.Dataset.Versions.Count();

                    // if mapping to entites type exist
                    if (MappingUtils.ExistMappingWithEntityFromRoot(
                        datasetVersion.Dataset.MetadataStructure.Id,
                        BExIS.Dim.Entities.Mapping.LinkElementType.MetadataStructure,
                        typeid))
                    {
                        //load metadata and searching for the entity Attrs
                        XDocument metadata = XmlUtility.ToXDocument(datasetVersion.Metadata);
                        IEnumerable<XElement> xelements = XmlUtility.GetXElementsByAttribute(EntityReferenceXmlAttribute.entityid.ToString(), metadata);

                        foreach (XElement e in xelements)
                        {
                            //get attributes from xml node
                            long xId = 0;
                            int xVersion = 0;
                            long xTypeId = 0;

                            if (Int64.TryParse(e.Attribute(EntityReferenceXmlAttribute.entityid.ToString()).Value.ToString(), out xId) &&
                                Int32.TryParse(e.Attribute(EntityReferenceXmlAttribute.entityversion.ToString()).Value.ToString(), out xVersion) &&
                                Int64.TryParse(e.Attribute(EntityReferenceXmlAttribute.entitytype.ToString()).Value.ToString(), out xTypeId)
                                )
                            {
                                //entityName = xmlDatasetHelper.GetEntityNameFromMetadatStructure(metadataStrutcureId, new Dlm.Services.MetadataStructure.MetadataStructureManager());
                                //entityType = entityManager.Entities.Where(e => e.Name.Equals(entityName)).FirstOrDefault();
                                string xpath = e.GetAbsoluteXPath();

                                tmp.Add(new EntityReference(
                                        id,
                                        typeid,
                                        version,
                                        xId,
                                        xTypeId,
                                        xVersion,
                                        xpath,
                                        DefaultEntitiyReferenceType.MetadataLink.GetDisplayName()
                                    ));
                            }
                        }
                    }
                }

                return tmp;

            }
        }

        private void setAdditionalFunctions()
        {
            TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];

            //set function actions of COPY, RESET,CANCEL,SUBMIT
            ActionInfo copyAction = new ActionInfo();
            copyAction.ActionName = "Copy";
            copyAction.ControllerName = "CreateSample";
            copyAction.AreaName = "RDB";

            ActionInfo resetAction = new ActionInfo();
            resetAction.ActionName = "Reset";
            resetAction.ControllerName = "Form";
            resetAction.AreaName = "DCM";

            ActionInfo cancelAction = new ActionInfo();
            cancelAction.ActionName = "Cancel";
            cancelAction.ControllerName = "Form";
            cancelAction.AreaName = "DCM";

            ActionInfo submitAction = new ActionInfo();
            submitAction.ActionName = "Submit";
            submitAction.ControllerName = "CreateSample";
            submitAction.AreaName = "RDB";


            TaskManager.Actions.Add(CreateTaskmanager.CANCEL_ACTION, cancelAction);
            TaskManager.Actions.Add(CreateTaskmanager.COPY_ACTION, copyAction);
            TaskManager.Actions.Add(CreateTaskmanager.RESET_ACTION, resetAction);
            TaskManager.Actions.Add(CreateTaskmanager.SUBMIT_ACTION, submitAction);

        }

        private DatasetVersion setStateInfo(DatasetVersion workingCopy, bool valid)
        {
            //StateInfo
            if (workingCopy.StateInfo == null) workingCopy.StateInfo = new Vaiona.Entities.Common.EntityStateInfo();

            if (valid)
                workingCopy.StateInfo.State = DatasetStateInfo.Valid.ToString();
            else workingCopy.StateInfo.State = DatasetStateInfo.NotValid.ToString();

            return workingCopy;
        }

        private DatasetVersion setModificationInfo(DatasetVersion workingCopy, bool newDataset, string user, string comment)
        {
            // modifikation info
            if (workingCopy.StateInfo == null) workingCopy.ModificationInfo = new EntityAuditInfo();

            if (newDataset)
                workingCopy.ModificationInfo.ActionType = AuditActionType.Create;
            else
                workingCopy.ModificationInfo.ActionType = AuditActionType.Edit;

            //set performer
            workingCopy.ModificationInfo.Performer = string.IsNullOrEmpty(user) ? "" : user;

            //set comment
            workingCopy.ModificationInfo.Comment = string.IsNullOrEmpty(comment) ? "" : comment;

            //set time
            workingCopy.ModificationInfo.Timestamp = DateTime.Now;

            return workingCopy;
        }


        #endregion

    }
}
