using System.Web.Mvc;

namespace BExIS.Web.Shell.Areas.RDB
{
    public class RDBAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "RDB";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "RDB_default",
                "RDB/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}