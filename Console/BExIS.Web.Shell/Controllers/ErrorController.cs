﻿using System.Web.Mvc;

namespace BExIS.Web.Shell.Controllers
{
    public class ErrorController : Controller
    {
        // GET: Error
        public ActionResult AccessDenied()
        {
            return View();
        }
    }
}