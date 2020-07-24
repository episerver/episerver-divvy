using EPiServer.Core;
using System.Configuration;

namespace Episerver.Labs.Divvy.Config
{
    public class DivvyMappingConfig : ConfigurationElement
    {
        [ConfigurationProperty("divvyType", IsKey = true, IsRequired = true)]
        public string DivvyType
        {
            get
            {
                return (string)base["divvyType"];
            }
        }

        [ConfigurationProperty("episerverType", IsKey = false, IsRequired = true)]
        public string EpiserverType
        {
            get
            {
                return (string)base["episerverType"];
            }
        }

        [ConfigurationProperty("parent", IsKey = false, IsRequired = true)]
        public ContentReference Parent
        {
            get
            {
                return new ContentReference(int.Parse(base["parent"].ToString()));
            }
        }
    }
}