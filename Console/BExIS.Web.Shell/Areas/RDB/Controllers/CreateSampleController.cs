﻿using BExIS.Dcm.CreateDatasetWizard;
using BExIS.Dcm.UploadWizard;
using BExIS.Dcm.Wizard;
using BExIS.Ddm.Api;
using BExIS.Dlm.Entities.Administration;
using BExIS.Dlm.Entities.Data;
using BExIS.Dlm.Entities.DataStructure;
using BExIS.Dlm.Entities.MetadataStructure;
using BExIS.Dlm.Services.Administration;
using BExIS.Dlm.Services.Data;
using BExIS.Dlm.Services.DataStructure;
using BExIS.Dlm.Services.MetadataStructure;
using BExIS.Modules.Rdb.UI.Models;
using BExIS.Security.Entities.Authorization;
using BExIS.Security.Entities.Subjects;
using BExIS.Security.Services.Authorization;
using BExIS.Security.Services.Subjects;
using BExIS.Web.Shell.Helpers;
using BExIS.Web.Shell.Models;
using BExIS.Xml.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;
using Vaiona.IoC;
using Vaiona.Logging;
using Vaiona.Web.Extensions;
using Vaiona.Web.Mvc.Models;

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

                Session["CreateDatasetTaskmanager"] = TaskManager;
                Session["MetadataStructureViewList"] = LoadMetadataStructureViewList();
                Session["DataStructureViewList"] = LoadDataStructureViewList();
                Session["DatasetViewList"] = LoadDatasetViewList();


                setAdditionalFunctions();

                //set Entity to TaskManager
                TaskManager.AddToBus(CreateTaskmanager.ENTITY_CLASS_PATH, "BExIS.Rdb.Entities.Sample");

                SetupModel Model = GetDefaultModel();

                //if id is set and its type dataset
                if (id != -1 && type.ToLower().Equals("datasetid"))
                {
                    ViewBag.Title = PresentationModel.GetViewTitleForTenant("Copy Sample", this.Session.GetTenant());

                    DatasetManager datasetManager = new DatasetManager();
                    Dataset dataset = datasetManager.DatasetRepo.Get(id);
                    Model.SelectedDatasetId = id;
                    Model.SelectedMetadataStructureId = dataset.MetadataStructure.Id;
                    Model.SelectedDataStructureId = dataset.DataStructure.Id;
                    Model.BlockMetadataStructureId = true;
                    Model.BlockDatasetId = true;
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
            DatasetManager datasetManager = new DatasetManager();
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

        public ActionResult StoreSelectedDatasetSetup(SetupModel model)
        {
            CreateTaskmanager TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];
            DatasetManager dm = new DatasetManager();

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

        [HttpPost]
        public ActionResult StoreSelectedDataset(long id)
        {
            if (TaskManager == null) TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];

            DatasetManager dm = new DatasetManager();
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

        [HttpGet]
        public ActionResult ShowListOfDataStructures()
        {
            List<ListViewItemWithType> datastructures = LoadDataStructureViewList();

            EntitySelectorModel model = BexisModelManager.LoadEntitySelectorModel(
                 datastructures, new List<string> { "Id", "Title", "Description", "Type" },
                 new EntitySelectorModelAction("ShowListOfDataStructuresReciever", "CreateSample", "RDB"));

            model.Title = "Select a Data Structure";


            return PartialView("_EntitySelectorInWindowView", model);
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
            model.BlockDatastructureId = false;
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
            if ((List<ListViewItemWithType>)Session["DataStructureViewList"] != null) model.DataStructureViewList = (List<ListViewItemWithType>)Session["DataStructureViewList"];
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

        public ActionResult Submit()
        {
            // create and submit Dataset 
            long datasetId = SubmitSample();

            bool editMode = false;

            if (TaskManager == null) TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];

            if (TaskManager.Bus.ContainsKey(CreateTaskmanager.EDIT_MODE))
                editMode = (bool)TaskManager.Bus[CreateTaskmanager.EDIT_MODE];

            if (editMode)
                return RedirectToAction("LoadMetadata", "Form", new { area = "DCM", entityId = datasetId, locked = true, created = false, fromEditMode = true });
            else
                return RedirectToAction("LoadMetadata", "Form", new { area = "DCM", entityId = datasetId, locked = true, created = true });
        }

        /// <summary>
        /// Submit a Dataset based on the imformations
        /// in the CreateTaskManager
        /// </summary>
        public long SubmitSample()
        {
            #region create sample

            TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];

            if (TaskManager.Bus.ContainsKey(CreateTaskmanager.DATASTRUCTURE_ID)
                && TaskManager.Bus.ContainsKey(CreateTaskmanager.RESEARCHPLAN_ID)
                && TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATASTRUCTURE_ID))
            {
                DatasetManager dm = new DatasetManager();
                long datasetId = 0;
                // for e new dataset
                if (!TaskManager.Bus.ContainsKey(CreateTaskmanager.ENTITY_ID))
                {
                    long datastructureId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.DATASTRUCTURE_ID]);
                    long researchPlanId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.RESEARCHPLAN_ID]);
                    long metadataStructureId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.METADATASTRUCTURE_ID]);

                    DataStructureManager dsm = new DataStructureManager();

                    DataStructure dataStructure = dsm.StructuredDataStructureRepo.Get(datastructureId);
                    //if datastructure is not a structured one
                    if (dataStructure == null) dataStructure = dsm.UnStructuredDataStructureRepo.Get(datastructureId);

                    ResearchPlanManager rpm = new ResearchPlanManager();
                    ResearchPlan rp = rpm.Repo.Get(researchPlanId);

                    MetadataStructureManager msm = new MetadataStructureManager();
                    MetadataStructure metadataStructure = msm.Repo.Get(metadataStructureId);

                    var ds = dm.CreateEmptyDataset(dataStructure, rp, metadataStructure);
                    datasetId = ds.Id;

                    // add security
                    // add security
                    if (GetUsernameOrDefault() != "DEFAULT")
                    {
                        EntityPermissionManager entityPermissionManager = new EntityPermissionManager();
                        entityPermissionManager.Create<User>(GetUsernameOrDefault(), "Sample", typeof(Dataset), ds.Id, Enum.GetValues(typeof(RightType)).Cast<RightType>().ToList());
                    }

                }
                else
                {
                    datasetId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.ENTITY_ID]);
                }

                TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];

                if (dm.IsDatasetCheckedOutFor(datasetId, GetUsernameOrDefault()) || dm.CheckOutDataset(datasetId, GetUsernameOrDefault()))
                {
                    DatasetVersion workingCopy = dm.GetDatasetWorkingCopy(datasetId);

                    if (TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATA_XML))
                    {
                        XDocument xMetadata = (XDocument)TaskManager.Bus[CreateTaskmanager.METADATA_XML];
                        workingCopy.Metadata = XmlMetadataWriter.ToXmlDocument(xMetadata);
                    }

                    string title = xmlDatasetHelper.GetInformation(datasetId, NameAttributeValues.title);
                    if (String.IsNullOrEmpty(title)) title = "No Title available.";

                    TaskManager.AddToBus(CreateTaskmanager.ENTITY_TITLE, title);//workingCopy.Metadata.SelectNodes("Metadata/Description/Description/Title/Title")[0].InnerText);
                    TaskManager.AddToBus(CreateTaskmanager.ENTITY_ID, datasetId);

                    dm.EditDatasetVersion(workingCopy, null, null, null);
                    dm.CheckInDataset(datasetId, "Metadata was submited.", GetUsernameOrDefault());

                    //add to index
                    // ToDo check which SearchProvider it is, default luceneprovider
                    ISearchProvider provider = IoCFactory.Container.ResolveForSession<ISearchProvider>() as ISearchProvider;
                    provider?.UpdateSingleDatasetIndex(datasetId, IndexingAction.CREATE);

                    LoggerFactory.LogData(datasetId.ToString(), typeof(Dataset).Name, Vaiona.Entities.Logging.CrudState.Created);

                }

                return datasetId;
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

            BExIS.Dcm.UploadWizard.DataStructureType type = new BExIS.Dcm.UploadWizard.DataStructureType();

            if (TaskManager.Bus.ContainsKey(CreateTaskmanager.DATASTRUCTURE_TYPE))
            {
                type = (BExIS.Dcm.UploadWizard.DataStructureType)TaskManager.Bus[CreateTaskmanager.DATASTRUCTURE_TYPE];
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
                DatasetManager dm = new DatasetManager();
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

            return RedirectToAction("StartMetadataEditor", "Form", new { area = "DCM" });
        }

        public ActionResult Reset()
        {
            //public ActionResult LoadMetadata(long datasetId, bool locked = false, bool created = false, bool fromEditMode = false, bool resetTaskManager = false, XmlDocument newMetadata = null)

            TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];
            if (TaskManager != null)
            {
                DatasetManager dm = new DatasetManager();
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
            DataStructureManager dataStructuremanager = new DataStructureManager();
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

        public List<ListViewItem> LoadMetadataStructureViewList()
        {
            MetadataStructureManager msm = new MetadataStructureManager();
            List<ListViewItem> temp = new List<ListViewItem>();

            foreach (MetadataStructure metadataStructure in msm.Repo.Get())
            {
                if (xmlDatasetHelper.IsActive(metadataStructure.Id) &&
                    xmlDatasetHelper.HasEntityType(metadataStructure.Id, "BExIS.Dlm.Entities.Data.Dataset", "Sample"))
                {
                    string title = metadataStructure.Name;

                    temp.Add(new ListViewItem(metadataStructure.Id, title, metadataStructure.Description));
                }
            }

            return temp.OrderBy(p => p.Title).ToList();
        }

        public List<ListViewItemWithType> LoadDataStructureViewList()
        {
            DataStructureManager dsm = new DataStructureManager();
            List<ListViewItemWithType> temp = new List<ListViewItemWithType>();

            foreach (DataStructure dataStructure in dsm.AllTypesDataStructureRepo.Get())
            {
                string title = dataStructure.Name;
                string type = "";
                if (dataStructure is StructuredDataStructure)
                {
                    type = "structured";
                }

                if (dataStructure is UnStructuredDataStructure)
                {
                    type = "unstructured";
                }

                temp.Add(new ListViewItemWithType(dataStructure.Id, title, dataStructure.Description, type));
            }

            return temp.OrderBy(p => p.Title).ToList();
        }

        public List<ListViewItem> LoadDatasetViewList()
        {
            List<ListViewItem> temp = new List<ListViewItem>();

            DatasetManager datasetManager = new DatasetManager();
            EntityPermissionManager entityPermissionManager = new EntityPermissionManager();
            //get all datasetsid where the current userer has access to
            UserManager userManager = new UserManager();
            XmlDatasetHelper xmlDatasetHelper = new XmlDatasetHelper();

            try
            {

                List<long> datasetIds = entityPermissionManager.GetKeys(GetUsernameOrDefault(), "Dataset",
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
            finally
            {
                datasetManager.Dispose();
                entityPermissionManager.Dispose();
                userManager.Dispose();
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

        #endregion

    }
}
