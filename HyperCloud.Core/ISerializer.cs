namespace HyperCloud
{
    public interface ISerializer
    {
        string ContentType { get; }
        byte[] MessageToBytes<T>(T message);
        T BytesToMessage<T>(byte[] bytes);
    }
}