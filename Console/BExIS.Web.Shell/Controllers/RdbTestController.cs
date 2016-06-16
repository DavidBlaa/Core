using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BExIS.Web.Shell.Helpers;
using BExIS.Web.Shell.Models;
using BEXIS.Rdb.Entities;
using BEXIS.Rdb.Helper;

namespace BExIS.Web.Shell.Controllers
{
    public class RdbTestController : Controller
    {
        public ActionResult Index()
        {

            Dictionary<string, EntitySelectorModel> listOfEntities = new Dictionary<string, EntitySelectorModel>();
            RdbCsvReader reader = new RdbCsvReader();

            //sites
            List<Site> sites = reader.ReadSiteCsv();
            EntitySelectorModel m = BexisModelManager.LoadEntitySelectorModel(sites);
            m.Title = "Sites";
            listOfEntities.Add(m.Title, m);

            //sites
            List<Person> person = reader.ReadPersonCsv();
            EntitySelectorModel pperson = BexisModelManager.LoadEntitySelectorModel(person);
            pperson.Title = "Person";
            listOfEntities.Add(pperson.Title, pperson);


            //plots
            List<Plot> plots = reader.ReadPlotCsv();
            EntitySelectorModel p = BexisModelManager.LoadEntitySelectorModel(plots);
            p.Title = "Plots";
            listOfEntities.Add(p.Title, p);

            //projects
            List<Project> projects = reader.ReadProjectCsv();
            EntitySelectorModel pr = BexisModelManager.LoadEntitySelectorModel(projects);
            pr.Title = "Projects";
            listOfEntities.Add(pr.Title, pr);

            //trees
            List<Tree> trees = reader.ReadTreeCsv();
            EntitySelectorModel t = BexisModelManager.LoadEntitySelectorModel(trees);
            t.Title = "Trees";
            listOfEntities.Add(t.Title, t);

            RdbTestModel model = new RdbTestModel();
            model.ListOfEntites = listOfEntities;
            model.Trees = trees;

            return View("RdbOverview",model);
        }
    }
}