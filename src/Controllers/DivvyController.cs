using Episerver.Labs.Divvy.ActionFilters;
using System.IO;
using System.Web.Mvc;

namespace Episerver.Labs.Divvy.Controllers
{
    public class DivvyController : Controller
    {
        [DivvySecurityFilter]
        [DivvyLogKeyFilter]
        [HttpPost]
        [Route("divvy/gateway")]
        public JsonResult Gateway()
        {
            if (DivvyManager.Settings.LogRequestDebugData)
            {
                DivvyLogManager.Log("Gateway Request Started", new { RequestKey = DivvyLogManager.GetRequestLogKey() });
            }

            var requestBody = GetRequestBody();
            var responseBody = DivvyManager.ProcessDivvyInput(requestBody);

            if (DivvyManager.Settings.LogRequestDebugData)
            {
                DivvyLogManager.Log("Gateway Request Ended");
            }

            return Json(responseBody);
        }

        private string GetRequestBody()
        {
            // We retrieve and parse this manually so we can run events on it...
            var bodyStream = new StreamReader(Request.InputStream);
            bodyStream.BaseStream.Seek(0, SeekOrigin.Begin);
            var bodyText = bodyStream.ReadToEnd();
            DivvyLogManager.LogRequest($"Request Body Retrieved ({bodyText.Length} byte(s). {bodyText})");
            return bodyText;
        }
    }
}