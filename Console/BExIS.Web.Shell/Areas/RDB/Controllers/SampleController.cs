using BExIS.Dlm.Entities.Data;
using BExIS.Dlm.Services.Data;
using BExIS.Modules.Rdb.UI.Models;
using BExIS.Security.Entities.Authorization;
using BExIS.Security.Services.Authorization;
using BExIS.Xml.Helpers;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml;
using Vaiona.Web.Extensions;
using Vaiona.Web.Mvc.Models;
using Vaiona.Web.Mvc.Modularity;

namespace BExIS.Modules.Rdb.UI.Controllers
{
    public class SampleController : Controller
    {

        private XmlDatasetHelper xmlDatasetHelper = new XmlDatasetHelper();
        // GET: Sample
        public ActionResult Index()
        {
            return View();
        }

        // GET: show
        public ActionResult Show(long id)
        {

            DatasetManager dm = new DatasetManager();
            EntityPermissionManager entityPermissionManager = new EntityPermissionManager();

            try
            {
                DatasetVersion dsv;
                ShowModel model = new ShowModel();

                string title = "";
                long metadataStructureId = -1;
                long dataStructureId = -1;
                long researchPlanId = 1;
                XmlDocument metadata = new XmlDocument();

                if (dm.IsDatasetCheckedIn(id))
                {
                    long versionId = dm.GetDatasetLatestVersionId(id); // check for zero value
                    dsv = dm.DatasetVersionRepo.Get(versionId); // this is needed to allow dsv to access to an open session that is available via the repo

                    metadataStructureId = dsv.Dataset.MetadataStructure.Id;

                    //MetadataStructureManager msm = new MetadataStructureManager();
                    //dsv.Dataset.MetadataStructure = msm.Repo.Get(dsv.Dataset.MetadataStructure.Id);

                    title = xmlDatasetHelper.GetInformationFromVersion(dsv.Id, NameAttributeValues.title); // this function only needs metadata and extra fields, there is no need to pass the version to it.
                    dataStructureId = dsv.Dataset.DataStructure.Id;
                    researchPlanId = dsv.Dataset.ResearchPlan.Id;
                    metadata = dsv.Metadata;

                    ViewBag.Title = PresentationModel.GetViewTitleForTenant("Show Data : " + title, this.Session.GetTenant());
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Dataset is just in processing.");
                }

                model = new ShowModel()
                {
                    Id = id,
                    Title = title,
                    MetadataStructureId = metadataStructureId,
                    DataStructureId = dataStructureId,
                    ResearchPlanId = researchPlanId,
                    ViewAccess = entityPermissionManager.HasEffectiveRight(HttpContext.User.Identity.Name, "Sample", typeof(Dataset), id, RightType.Read),
                    GrantAccess = entityPermissionManager.HasEffectiveRight(HttpContext.User.Identity.Name, "Sample", typeof(Dataset), id, RightType.Grant)
                };

                //set metadata in session
                Session["ShowDataMetadata"] = metadata;

                return View(model);
            }
            finally
            {
                dm.Dispose();
                entityPermissionManager.Dispose();
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="datasetID"></param>
        /// <returns>model</returns>
        public ActionResult ShowMetaData(long entityId, string title, long metadatastructureId, long datastructureId, long researchplanId, string sessionKeyForMetadata)
        {
            var result = this.Run("DCM", "Form", "SetAdditionalFunctions", new RouteValueDictionary() { { "actionName", "Copy" }, { "controllerName", "CreateDataset" }, { "area", "DCM" }, { "type", "copy" } });
            result = this.Run("DCM", "Form", "SetAdditionalFunctions", new RouteValueDictionary() { { "actionName", "Reset" }, { "controllerName", "Form" }, { "area", "Form" }, { "type", "reset" } });
            result = this.Run("DCM", "Form", "SetAdditionalFunctions", new RouteValueDictionary() { { "actionName", "Cancel" }, { "controllerName", "Form" }, { "area", "DCM" }, { "type", "cancel" } });
            result = this.Run("DCM", "Form", "SetAdditionalFunctions", new RouteValueDictionary() { { "actionName", "Submit" }, { "controllerName", "CreateDataset" }, { "area", "DCM" }, { "type", "submit" } });

            var view = this.Render("DCM", "Form", "LoadMetadataFromExternal", new RouteValueDictionary()
            {
                { "entityId", entityId },
                { "title", title },
                { "metadatastructureId", metadatastructureId },
                { "datastructureId", datastructureId },
                { "researchplanId", researchplanId },
                { "sessionKeyForMetadata", sessionKeyForMetadata },
                { "resetTaskManager", false }
            });

            return Content(view.ToHtmlString(), "text/html");
        }
    }
}