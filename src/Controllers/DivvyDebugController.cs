using Episerver.Labs.Divvy.ActionFilters;
using EPiServer;
using EPiServer.ServiceLocation;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Episerver.Labs.Divvy.Controllers
{
    [DivvySecurityFilter]
    public class DivvyDebugController : Controller
    {
        [HttpGet]
        [Route("divvy/debug/log")]
        public JsonResult RequestLog()
        {
            return Json(DivvyLogManager.GetLogEntries(Guid.Empty), JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        [Route("divvy/debug/log/{key}")]
        public JsonResult RequestLog(Guid key)
        {
            return Json(DivvyLogManager.GetLogEntries(key), JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        [Route("divvy/debug/mappings")]
        public JsonResult Mappings()
        {
            return Json(DivvyContentMapping.FindAll(), JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        [Route("divvy/debug/types")]
        public JsonResult Types()
        {
            return Json(DivvyManager.Settings.TypeMappings, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        [Route("divvy/debug/mappings/clear")]
        public JsonResult ClearMappings()
        {
            DivvyContentMapping.DeleteAll();
            return Json("OK", JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        [Route("divvy/debug/mappings/clear/{key}")]
        public JsonResult ClearMapping(Guid key)
        {
            DivvyContentMapping.Delete(key);
            return Json("OK", JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        [Route("divvy/debug/log/on")]
        public JsonResult TurnLoggingOn()
        {
            DivvyManager.Settings.LogRequestDebugData = true;
            return Json("Logging is on.", JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        [Route("divvy/debug/log/off")]
        public JsonResult TurnLoggingOff()
        {
            DivvyManager.Settings.LogRequestDebugData = false;
            DivvyLogManager.Clear();
            return Json("Logging is off. Log entries deleted.", JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        [Route("divvy/debug/log/status")]
        public JsonResult LoggingStatus()
        {
            return Json(DivvyManager.Settings.LogRequestDebugData ? "Logging is on." : "Logging is off.", JsonRequestBehavior.AllowGet);
        }
    }
}