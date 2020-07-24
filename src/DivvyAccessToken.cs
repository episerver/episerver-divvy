using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Episerver.Labs.Divvy
{
    public static class DivvyAccessToken
    {
        private const string AUTH_HEADER_NAME = "Authorization";
        private const string QUERYSTRING_NAME = "auth";

        public static ValidationResult Validate()
        {
            DivvyManager.Init();  // This will immediately return if Init has already occured

            // ValidationResult defaults to Authorized: False

            if (DivvyManager.Settings.Mode == DivvyMode.Disabled)
            {
                return new ValidationResult() { Message = "Divvy disabled. Divvy integration is installed but not enabled in this Episerver instance." };
            }

            if (!HasToken())
            {
                return new ValidationResult() { Message = "Missing auth token." };
            }

            var incomingToken = GetTokenString();

            // The token has to parse as a GUID
            if (!Guid.TryParse(incomingToken, out Guid parsedToken))
            {
                return new ValidationResult() { Message = $"Malformed auth token. Token must be a 32-digit GUID/UUID. Token: {incomingToken}" };
            }

            // This value has to match what's in the web.config: /configuration/divvy/@authToken
            if (parsedToken != DivvyManager.Settings.AccessToken)
            {
                return new ValidationResult() { Message = $"Not authorized. Auth token was present, but is not authorized for access. Token: {incomingToken}" };
            }

            // .. if we got here, we're good.
            return new ValidationResult() { Authorized = true };
        }


        private static bool HasToken()
        {
            return HttpContext.Current.Request.Headers[AUTH_HEADER_NAME] != null || HttpContext.Current.Request.QueryString[QUERYSTRING_NAME] != null;
        }

        private static string GetTokenString()
        {
            var raw = HttpContext.Current.Request.Headers[AUTH_HEADER_NAME] ?? HttpContext.Current.Request.QueryString[QUERYSTRING_NAME] ?? null;
            if(raw != null)
            {
                raw = raw.Trim().Split(" ".ToCharArray()).Last().Trim(); // If it's the header, it will need to be everything after the space
            }
            return raw;
        }
    }

    public struct ValidationResult
    {
        public bool Authorized { get; set; }
        public string Message { get; set; }
    }
}