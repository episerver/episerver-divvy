using System;
using System.Web;
using System.Web.Mvc;

namespace Episerver.Labs.Divvy.ActionFilters
{
    public class DivvyLogKeyFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            HttpContext.Current.Items.Add(DivvyLogManager.LogKeyItemKey, Guid.NewGuid());
        }
    }
}