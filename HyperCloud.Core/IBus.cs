using System;
using System.Threading.Tasks;

namespace HyperCloud
{
    public interface IBus : IDisposable
    {
        void Publish<T>(T message, string address = "");
        void Subscribe<T>(string subscription, Action<T> onMessage, SubscribeOptions options = null);
        void SubscribeAsync<T>(string subscription, Func<T, Task> onMessage, SubscribeOptions options = null);

        event Action Connected;
        event Action Disconnected;
        bool IsConnected { get; }
    }
}