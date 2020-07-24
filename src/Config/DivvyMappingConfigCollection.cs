using System.Configuration;

namespace Episerver.Labs.Divvy.Config
{
    public class DivvyMappingConfigCollection : ConfigurationElementCollection
    {
        public DivvyMappingConfig this[int index]
        {
            get
            {
                return (DivvyMappingConfig)BaseGet(index);
            }

        }

        public new DivvyMappingConfig this[string key]
        {
            get
            {
                return (DivvyMappingConfig)BaseGet(key);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new DivvyMappingConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((DivvyMappingConfig)element).DivvyType;
        }
    }

}