using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Configuration;

namespace HyperCloud.Configuration
{
    public class BusSection : ConfigurationSection
    {
        [ConfigurationProperty("connectionStringName", IsRequired = true)]
        public string ConnectionStringName
        {
            get { return (string)this["connectionStringName"]; }
            set { this["connectionStringName"] = value; }
        }

        [ConfigurationProperty("durableQueues", IsRequired = false, DefaultValue=true)]
        public bool DurableQueues
        {
            get { return (bool)this["durableQueues"]; }
            set { this["durableQueues"] = value; }
        }

        [ConfigurationProperty("autodeleteQueues", IsRequired = false, DefaultValue = false)]
        public bool AutodeleteQueues
        {
            get { return (bool)this["autodeleteQueues"]; }
            set { this["autodeleteQueues"] = value; }
        }

        [ConfigurationProperty("serializers", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(SerializersCollection),
            AddItemName = "add",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public SerializersCollection Serializers
        {
            get { return (SerializersCollection)base["serializers"]; }
        }
    }

    public class SerializersCollection : ConfigurationElementCollection
    {
        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        public new SerializerElement this[string name]
        {
            get { return (SerializerElement)BaseGet(name); }
        }

        public SerializerElement this[int index]
        {
            get { return (SerializerElement)BaseGet(index); }
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
            get { return "serializer"; }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new SerializerElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((SerializerElement)element).Name;
        }
    }

    public class SerializerElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("type", IsRequired = true)]
        public Type SerizlierType
        {
            get { return (Type)this["type"]; }
            set { this["type"] = value; }
        }
    }

}
