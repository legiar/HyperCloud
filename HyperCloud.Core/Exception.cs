using System;
using System.Runtime.Serialization;

namespace HyperCloud
{
    [Serializable]
    public class HyperCloudException : Exception
    {
        public HyperCloudException()
        {
        }

        public HyperCloudException(string message) 
            : base(message)
        {
        }

        public HyperCloudException(string format, params string[] args) 
            : base(string.Format(format, args))
        {
        }

        public HyperCloudException(string message, Exception inner) 
            : base(message, inner)
        {
        }

        protected HyperCloudException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class HyperAdminInvalidMessageTypeException : HyperCloudException
    {
        public HyperAdminInvalidMessageTypeException()
        {
        }

        public HyperAdminInvalidMessageTypeException(string message) 
            : base(message)
        {
        }

        public HyperAdminInvalidMessageTypeException(string format, params string[] args) 
            : base(format, args)
        {
        }

        public HyperAdminInvalidMessageTypeException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected HyperAdminInvalidMessageTypeException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}