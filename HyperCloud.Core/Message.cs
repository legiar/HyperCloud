using System;
using System.Collections;
using System.Text;

using RabbitMQ.Client.Events;

namespace HyperAdmin
{
    public class Message : IMessage
    {
        private string _contentType;
        private string _type;
        private string _messageId;
        private byte[] _body;
        
        //private string m_contentEncoding;
        //private System.Collections.IDictionary m_headers;
        //private byte m_deliveryMode;
        //private byte m_priority;
        //private string m_correlationId;
        //private string m_replyTo;
        //private string m_expiration;
        //private AmqpTimestamp m_timestamp;
        //private string m_type;
        //private string m_userId;
        //private string m_appId;
        //private string m_clusterId;

        //string ContentEncoding { get; set; }
        //IDictionary Headers { get; set; }
        //byte DeliveryMode { get; set; }
        //byte Priority { get; set; }
        //string CorrelationId { get; set; }
        //string ReplyTo { get; set; }
        //string Expiration { get; set; }
        //string UserId { get; set; }
        //string AppId { get; set; }
        //string ClusterId { get; set; }

        public Message()
        {
            _messageId = "";
            _type = "";
            _contentType = "";
        }

        public Message(BasicDeliverEventArgs msg)
            : this()
        {
            if (msg.BasicProperties.IsContentTypePresent())
            {
                _contentType = msg.BasicProperties.ContentType;
            }
            if (msg.BasicProperties.IsTypePresent())
            {
                _type = msg.BasicProperties.Type;
            }
            if (msg.BasicProperties.IsMessageIdPresent())
            {
                _messageId = msg.BasicProperties.MessageId;
            }
            _body = msg.Body;
        }

        public string ContentType
        { 
            get { return _contentType; }
            set { _contentType = value; }
        }

        public string Type {
            get { return _type; }
            set { _type = value; }
        }

        public string MessageId
        {
            get { return _messageId; }
            set { _messageId = value; }
        }

        public byte[] Body
        {
            get { return _body; }
            set { _body = value; }
        }

        public override string ToString()
        {
            if (ContentType == "text/plain")
            {
                return Encoding.UTF8.GetString(_body);
            }
            else
            {
                return String.Format("Message with content/type = {0}", _contentType);
            }
        }
    }
}
