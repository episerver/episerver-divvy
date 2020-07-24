using System;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Episerver.Labs.Divvy.ActionFilters
{
    public class DivvySecurityFilter : ActionFilterAttribute
    {
        private const string AUTH_HEADER_NAME = "Authorization";
        private const string QUERYSTRING_NAME = "auth";

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            DivvyManager.Init();  // This will immediately return if Init has already occured

            // If Divvy is disabled, no one is getting in
            if (DivvyManager.Settings.Mode == DivvyMode.Disabled)
            {
                DivvyLogManager.Log("Divvy Integration Not Enabled. Aborting.");
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Divvy disabled. Divvy integration is installed but not enabled in this Episerver instance.");
                return; // Reject
            }

            // If they're logged into the debug role, they're good...
            if (DivvyManager.Settings.DebugRole != null && filterContext.HttpContext.User.IsInRole(DivvyManager.Settings.DebugRole))
            {
                return; // Allow
            }

            // If their auth token validated, they're good
            var result = DivvyAccessToken.Validate();
            if (result.Authorized)
            {
                return; // Allow
            }
            
            DivvyLogManager.Log(result.Message);
            filterContext.Result = new HttpUnauthorizedResult(result.Message);
            return; // Default reject everything that gets here
        }
    }
}