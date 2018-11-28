using BExIS.Rdb.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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
    }
}