using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace HyperCloud.Configuration
{
    public class ProcessorsSection : ConfigurationSection
    {
        private static readonly ConfigurationPropertyCollection _properties = InitializeProperties();
        private static ConfigurationProperty _path;
        private static ConfigurationProperty _processors;

        [ConfigurationProperty("path", IsRequired = true)]
        public string Path
        {
            get { return (string)this["path"]; }
            set { this["path"] = value; }
        }

        public ProcessorsSectionCollection Processors
        {
            get { return (ProcessorsSectionCollection)this[_processors]; }
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get { return _properties; }
        }

        private static ConfigurationPropertyCollection InitializeProperties()
        {
            _path = new ConfigurationProperty("path", typeof(string), null);
            _processors = new ConfigurationProperty(
                string.Empty, typeof(ProcessorsSectionCollection), null,
                ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsDefaultCollection);

            return new ConfigurationPropertyCollection{
                _path,
                _processors
            };
        }
    }

    public class ProcessorsSectionCollection : ConfigurationElementCollection
    {
        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        public new ProcessorSectionElement this[string name]
        {
            get { return (ProcessorSectionElement)this.BaseGet(name); }
            set
            {
                ConfigurationElement element = this.BaseGet(name);
                if (element != null) {
                    this.BaseRemove(element);
                }
                this.BaseAdd(value);
            }
        }

        public ProcessorSectionElement this[int index]
        {
            get { return (ProcessorSectionElement)this.BaseGet(index); }
            set
            {
                if (this.BaseGet(index) != null)
                {
                    this.BaseRemoveAt(index);
                }
                this.BaseAdd(index, value);
            }
        }

        protected override string ElementName
        {
            get { return "processor"; }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ProcessorSectionElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as ProcessorSectionElement).Name;
        }
    }

    public class ProcessorSectionElement : ConfigurationElement
    {
        private static readonly ConfigurationPropertyCollection _properties = InitializeProperties();
        private static ConfigurationProperty _name;
        private static ConfigurationProperty _type;
        private static ConfigurationProperty _enabled;
        private static ConfigurationProperty _parameters;

        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this[_name]; }
            set { this[_name] = value; }
        }

        [ConfigurationProperty("type", IsRequired = true)]
        public Type ManagerType
        {
            get { return (Type)this[_type]; }
            set { this[_type] = value; }
        }

        [ConfigurationProperty("enabled", IsRequired = true)]
        public bool Enabled
        {
            get { return (bool)this[_enabled]; }
            set { this[_enabled] = value; }
        }

        [ConfigurationProperty("parameters")]
        public ParameterElementCollection Parameters
        {
            get { return (ParameterElementCollection)this[_parameters]; }
            set { this[_parameters] = value; }
        }


        protected override ConfigurationPropertyCollection Properties
        {
            get { return _properties; }
        }

        private static ConfigurationPropertyCollection InitializeProperties()
        {
            _name = new ConfigurationProperty("name", typeof(string), null, ConfigurationPropertyOptions.IsRequired);
            _type = new ConfigurationProperty("type", typeof(string), null, ConfigurationPropertyOptions.IsRequired);
            _enabled = new ConfigurationProperty("enabled", typeof(bool), true, ConfigurationPropertyOptions.None);
            _parameters = new ConfigurationProperty(
                "parameters", typeof(ParameterElementCollection), null,
                ConfigurationPropertyOptions.IsDefaultCollection);
            return new ConfigurationPropertyCollection {
                _name,
                _type,
                _enabled,
                _parameters
            };
        }
    }
}
