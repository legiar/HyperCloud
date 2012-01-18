using System;
using System.Collections;
using System.Collections.Generic;

namespace HyperCloud
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MessageTypeAttribute : System.Attribute
    {
        private readonly string _type;

        public string Type {
            get { return _type; }
        }

        public MessageTypeAttribute(string Type)
        {
            _type = Type;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class MessageSerializerAttribute : System.Attribute
    {
        private readonly Type _serializerType;
        private ISerializer _serializer;

        public ISerializer Serializer
        {
            get {
                if (_serializer == null)
                {
                    _serializer = _serializerType.GetConstructor(new Type[]{}).Invoke(new object[]{}) as ISerializer;
                }
                return _serializer;
            }
        }

        public MessageSerializerAttribute(Type SerializerType)
        {
            _serializerType = SerializerType;
        }
    }

    public interface IMessageHeader
    {
        string MessageId { get; set; }
        IDictionary Headers { get; set; }
    }

    [Serializable]
    public class Message : IMessageHeader
    {
        [NonSerialized]
        private string _messageId;
        [NonSerialized]
        private IDictionary _headers;

        public string MessageId
        {
            get { return _messageId; }
            set { _messageId = value; }
        }

        public IDictionary Headers
        {
            get
            {
                if (_headers == null)
                {
                    _headers = new Dictionary<string, string>() { };
                }
                return _headers;
            }
            set { _headers = value; }
        }
    }

    [Serializable]
    public class CommandMessage : Message
    {
        public string Command { get; set; }
        public string Parameters { get; set; }
    }
}
