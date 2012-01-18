using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Configuration;

namespace HyperCloud.Configuration
{
    public class HyperCloudGroup : ConfigurationSectionGroup
	{
        public System.Configuration.Configuration RootConfiguration { get; set; }

        [ConfigurationProperty("bus", IsRequired = true)]
        public ConfigurationSection BusConfigurationSection
        {
            get { return Sections["bus"]; }
        }

        [ConfigurationProperty("processors", IsRequired = true)]
        public ConfigurationSection ProcessorsConfigurationSection
        {
            get { return Sections["processors"]; }
        }

        [ConfigurationProperty("commands", IsRequired = true)]
        public ConfigurationSection CommandsConfigurationSection
        {
            get { return Sections["commands"]; }
        }
	}

    public class ParameterElementCollection : ConfigurationElementCollection
    {
        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMap;
            }
        }

        public new ParameterElement this[string name]
        {
            get { return (ParameterElement)this.BaseGet(name); }
            set
            {
                ConfigurationElement element = this.BaseGet(name);
                if (element != null)
                {
                    this.BaseRemove(element);
                }
                this.BaseAdd(value);
            }
        }

        public ParameterElement this[int index]
        {
            get
            {
                return (ParameterElement)this.BaseGet(index);
            }
            set
            {
                if (this.BaseGet(index) != null)
                {
                    this.BaseRemoveAt(index);
                }
                this.BaseAdd(index, value);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ParameterElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ParameterElement)element).Name;
        }

        protected override string ElementName
        {
            get { return "add"; }
        }
    }

    public class ParameterElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("value", IsRequired = true)]
        public string Value
        {
            get { return (string)this["value"]; }
            set { this["value"] = value; }
        }
    }

}
