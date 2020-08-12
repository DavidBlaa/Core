using BExIS.Dlm.Entities.Data;
using BExIS.Dlm.Entities.DataStructure;
using BExIS.Dlm.Services.Data;
using BExIS.Modules.Rdb.UI.Models;
using BExIS.Security.Entities.Authorization;
using BExIS.Security.Services.Authorization;
using BExIS.Security.Services.Objects;
using BExIS.Security.Services.Subjects;
using BExIS.Utils.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Telerik.Web.Mvc;
using Vaiona.Web.Extensions;
using Vaiona.Web.Mvc.Models;

namespace BExIS.Modules.Rdb.UI.Controllers
{
    public class DashboardController : Controller
    {
        // GET: Dashboard
        public ActionResult Index()
        {
            return View();
        }
        /// <summary>
        /// create the model of My Dataset table
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref="_CustomMyDatasetBinding"/>
        /// <param name="entityname">Name of entity</param>
        /// <param name="rightType">Type of right (write, delete, grant, read)</param>
        /// <param name="onlyTable">Return only table without header</param>
        /// <returns>model</returns>
        public ActionResult ShowMySamples(string entityname, string rightType, string onlyTable = "false")
        {
            ViewBag.Title = PresentationModel.GetViewTitleForTenant("Dashboard", this.Session.GetTenant());

            List<MySamplesModel> model = new List<MySamplesModel>();
            using (DatasetManager datasetManager = new DatasetManager())
            using (EntityPermissionManager entityPermissionManager = new EntityPermissionManager())
            using (UserManager userManager = new UserManager())
            using (EntityManager entityManager = new EntityManager())
            {
                var entity = entityManager.FindByName(entityname);
                var user = userManager.FindByNameAsync(GetUsernameOrDefault()).Result;

                var rightTypeId = RightType.Read;

                if (rightType == "write")
                {
                    rightTypeId = RightType.Write;
                }
                else if (rightType == "delete")
                {
                    rightTypeId = RightType.Delete;
                }
                else if (rightType == "grant")
                {
                    rightTypeId = RightType.Grant;
                }

                var userName = GetUsernameOrDefault();
                if (userName == "DEFAULT")
                {
                    ViewBag.userLoggedIn = false;
                    rightTypeId = RightType.Read;
                }
                else
                {
                    ViewBag.userLoggedIn = true;
                }


                List<long> datasetIds = entityPermissionManager.GetKeys(GetUsernameOrDefault(), entityname,
                       typeof(Dataset), rightTypeId);

                List<DatasetVersion> datasetVersions = datasetManager.GetDatasetLatestVersions(datasetIds, true);
                foreach (var dsv in datasetVersions)
                {

                    Object[] rowArray = new Object[8];
                    string isValid = "no";

                    string type = "file";
                    if (dsv.Dataset.DataStructure.Self is StructuredDataStructure)
                    {
                        type = "tabular";
                    }


                    if (dsv.Dataset.Status == DatasetStatus.CheckedIn)
                    {

                        string title = string.IsNullOrEmpty(dsv.Title) ? "" : dsv.Title;
                        string description = string.IsNullOrEmpty(dsv.Description) ? "" : dsv.Description;

                        if (dsv.StateInfo != null)
                        {
                            isValid = DatasetStateInfo.Valid.ToString().Equals(dsv.StateInfo.State) ? "yes" : "no";
                        }

                        rowArray[0] = Convert.ToInt64(dsv.Dataset.Id);
                        rowArray[1] = title;
                        rowArray[2] = description;
                        rowArray[3] = type;
                    }
                    else
                    {
                        rowArray[0] = Convert.ToInt64(dsv.Dataset.Id);
                        rowArray[1] = "";
                        rowArray[2] = "Sample is just in processing.";
                        rowArray[3] = type;
                    }

                    rowArray[7] = true;

                    model.Add(new MySamplesModel(
                       (long)rowArray[0],
                      (string)rowArray[1],
                       (string)rowArray[2],
                       (bool)rowArray[7],
                       isValid, (string)rowArray[3]));


                }
            }
            if (onlyTable == "true")
            {
                return PartialView("_mySamplesView", model);
            }
            else
            {
                return PartialView("_mySamplesViewHeader", model);
            }

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
    }
}