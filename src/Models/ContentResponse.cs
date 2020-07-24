using System;
using System.Collections.Generic;

namespace Episerver.Labs.Divvy.Models
{
    // Everything in this class represents OUTGOING data from an Episerver content object
    public class ContentResponse
    {
        public ContentResponse()
        {
            Media = new List<Media>();
        }

        public Guid Id { get; set; }
        public string EditUrl { get; set; }
        public string PreviewHtml { get; set; }
        public DateTime LastModified { get; set; }
        public List<Media> Media { get; set; }

        public Guid RequestLogKey => DivvyLogManager.GetRequestLogKey();
    }

    public class Media
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }
}