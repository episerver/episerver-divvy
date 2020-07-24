using Divvy;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Episerver.Labs.Divvy
{
    public static class DivvyLogManager
    {
        public static string LogKeyItemKey = "DIVVY_LOG_KEY";

        public static void LogRequest(string message, object value = null)
        {
            if (!DivvyManager.Settings.LogRequestDebugData)
            {
                return;
            }

            if(value != null)
            {
                message = string.Concat(message, ". ", SerializeObject(value));
            }

            var requestKey = GetRequestLogKey();
            if(requestKey == Guid.Empty)
            {
                Log("Attempted to log request data, but no request key found.");
                return;
            }
            DivvyLogEntry.Create(requestKey, message);
        }

        public static void Log(string message, object value = null)
        {
            if (value != null)
            {
                message = string.Concat(message, ". ", SerializeObject(value));
            }

            DivvyLogEntry.Create(Guid.Empty, message);
        }

        public static List<DivvyLogEntry> GetLogEntries(Guid requestKey)
        {
            return DivvyLogEntry.Get(requestKey);
        }

        public static void Clear()
        {
            DivvyLogEntry.Clear();
        }

        private static string SerializeObject(object value)
        {
            return string.Join(", ", value.GetType().GetProperties().Select(p => $"{p.Name}: \"{p.GetValue(value)}\""));
        }

        public static Guid GetRequestLogKey()
        {
            // This might return null in two places
            // 1. There might not be a current HttpContext, if this is the webhooks background thread
            // 2. This request might not have gotten a log key (debugging requests, for example)
            var requestKeyString = HttpContext.Current?.Items[LogKeyItemKey]?.ToString();
            if(requestKeyString == null)
            {
                // There was nothing in the request
                return Guid.Empty;
            }

            if(!Guid.TryParse(requestKeyString, out Guid result))
            {
                // There was something in the request, but it wasn't a GUID
                return Guid.Empty;
            }

            return result;

        }
    }
}