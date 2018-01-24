using BExIS.Modules.Rdb.UI.Models;
using BExIS.Rdb.Helper;
using BExIS.Web.Shell.Helpers;
using BExIS.Web.Shell.Models;
using GenCode128;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Web.Mvc;
using Vaiona.Utils.Cfg;


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

            //Soils
            EntitySelectorModel s = BexisModelManager.LoadEntitySelectorModel(importManager.Soils);
            s.Title = "Soils";
            listOfEntities.Add(s.Title, s);

            RdbTestModel model = new RdbTestModel();
            model.ListOfEntites = listOfEntities;
            model.Trees = importManager.Trees;
            model.Soils = importManager.Soils;

            return View(model);
        }

        public ActionResult Convert()
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

        public ActionResult GenerateBarCode()
        {
            string filepath;
            Image barcodeImage;
            try
            {
                filepath = Path.Combine(AppConfiguration.GetModuleWorkspacePath("RDB"), "test.jpg");
                string test = "123456";
                barcodeImage = Code128Rendering.MakeBarcodeImage(test, 2, true);
                barcodeImage.Save(filepath);

            }
            catch (Exception ex)
            {
                throw ex;
            }

            BarCodeModel model = new BarCodeModel();
            model.Image = barcodeImage;

            return View("BarcodeGeneratorView", model);
        }

    }
}