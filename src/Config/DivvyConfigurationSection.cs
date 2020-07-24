using System;
using System.Configuration;

namespace Episerver.Labs.Divvy.Config
{

    public class DivvyConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("enabled", IsKey = false, IsRequired = true)]
        public bool Enabled
        {
            get
            {
                return Convert.ToBoolean(base["enabled"]);
            }
        }

        [ConfigurationProperty("authToken", IsKey = false, IsRequired = true)]
        public Guid AuthToken
        {
            get
            {
                return Guid.Parse(base["authToken"].ToString());
            }
        }

        [ConfigurationProperty("debugRole", IsKey = false, IsRequired = false)]
        public string DebugRole
        {
            get
            {
                return base["debugRole"]?.ToString();
            }
        }

        [ConfigurationProperty("mappings")]
        [ConfigurationCollection(typeof(DivvyMappingConfigCollection), AddItemName = "mapping")]
        public DivvyMappingConfigCollection Mappings
        {
            get
            {
                // Get the collection and parse it
                return (DivvyMappingConfigCollection)this["mappings"];
            }
        }
    }
}