using EPiServer.Core;

namespace Episerver.Labs.Divvy
{
    public class DivvyTypeMapping
    {
        public string DivvyTypeName { get; set; }
        public string EpiserverPageTypeName { get; set; }
        public ContentReference ParentNode { get; set; }
    }
}