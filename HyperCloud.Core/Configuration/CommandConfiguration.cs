using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace HyperCloud.Configuration
{
    public class CommandsSection : ConfigurationSection
    {
        private static readonly ConfigurationPropertyCollection _properties = InitializeProperties();
        private static ConfigurationProperty _commands;

        public CommandsSectionCollection Commands
        {
            get { return (CommandsSectionCollection)this[_commands]; }
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get { return _properties; }
        }

        private static ConfigurationPropertyCollection InitializeProperties()
        {
            _commands = new ConfigurationProperty(
                string.Empty, typeof(CommandsSectionCollection), null,
                ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsDefaultCollection);

            return new ConfigurationPropertyCollection{
                _commands
            };
        }
    }

    public class CommandsSectionCollection : ConfigurationElementCollection
    {
        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        public new CommandSectionElement this[string name]
        {
            get { return (CommandSectionElement)this.BaseGet(name); }
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

        public CommandSectionElement this[int index]
        {
            get { return (CommandSectionElement)this.BaseGet(index); }
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
            get { return "command"; }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new CommandSectionElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as CommandSectionElement).Name;
        }
    }

    public class CommandSectionElement : ConfigurationElement
    {
        private static readonly ConfigurationPropertyCollection _properties = InitializeProperties();
        private static ConfigurationProperty _name;
        private static ConfigurationProperty _parameters;

        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this[_name]; }
            set { this[_name] = value; }
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
            _parameters = new ConfigurationProperty(
                "parameters", typeof(ParameterElementCollection), null,
                ConfigurationPropertyOptions.IsDefaultCollection);
            return new ConfigurationPropertyCollection {
                _name,
                _parameters
            };
        }
    }
}
