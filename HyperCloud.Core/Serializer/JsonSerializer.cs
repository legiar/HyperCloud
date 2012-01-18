using System.Text;
using ServiceStack.Text;

namespace HyperCloud.Serializer
{
    public class JsonSerializer : ISerializer
    {
        public string ContentType
        {
            get { return "application/json"; }
        }

        public byte[] MessageToBytes<T>(T message)
        {
            return Encoding.UTF8.GetBytes(ServiceStack.Text.JsonSerializer.SerializeToString<T>(message));
        }

        public T BytesToMessage<T>(byte[] bytes)
        {
            return ServiceStack.Text.JsonSerializer.DeserializeFromString<T>(Encoding.UTF8.GetString(bytes));
        }
    }
}