using Episerver.Labs.Divvy.Models;
using EPiServer.Core;
using System;

namespace Episerver.Labs.Divvy
{
    public class DivvyEventArgs : EventArgs
    {
        public string RawInput { get; set; }
        public bool CancelAction { get; set; }
        public ContentRequest ContentRequest { get; set; }
        public string PreviewHtml { get; set; }
        public ContentReference IntendedParent { get; set; }
        public string IntendedTypeName { get; set; }
    }
}