using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Episerver.Labs.Divvy.Models
{
    // Everything in this class represents INCOMING data from a Divvy content object
    public class ContentRequest
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ContentType { get; set; }
        public string Description { get; set; }
        public string Calendar { get; set; }
        public string Owner { get; set; }

        public static ContentRequest ParseJson(string json)
        {
            var jobject = JObject.Parse(json);
            var id = (string)jobject.SelectToken("id");
            var title = (string)jobject.SelectToken("title");
            var contentType = (string)jobject.SelectToken("content_type");

            DivvyLogManager.LogRequest("JSON parsed successfully.");

            var contentTypePrefix = "Content Type: ";
            if (contentType != null && contentType.StartsWith(contentTypePrefix))
            {
                contentType = new String(contentType.Skip(contentTypePrefix.Length).ToArray());
            }

            var contentRequest = new ContentRequest()
            {
                Id = int.Parse(id),
                Title = title,
                ContentType = contentType,
                Description = (string)jobject.SelectToken("description"),
                Calendar = (string)jobject.SelectToken("calendar"),
                Owner = (string)jobject.SelectToken("owner_member")
            };

            DivvyLogManager.LogRequest("Parsed Values", contentRequest);

            return contentRequest;
        }
    }
}