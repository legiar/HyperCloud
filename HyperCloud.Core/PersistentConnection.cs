using System;
using System.Collections.Generic;
using System.Threading;

using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace HyperCloud
{
    public interface IPersistentConnection : IDisposable
    {
        event Action Connected;
        event Action Disconnected;

        bool IsConnected { get; }

        IModel CreateModel();

        void AddSubscriptionAction(Action subscriptionAction);
    }

    public class PersistentConnection : IPersistentConnection
    {
        private const int connectAttemptIntervalMilliseconds = 5000;

        private readonly ConnectionFactory connectionFactory;
        private readonly ILogger logger;
        private IConnection connection;
        private readonly List<Action> subscribeActions;

        public PersistentConnection(ConnectionFactory connectionFactory, ILogger logger)
        {
            this.connectionFactory = connectionFactory;
            this.logger = logger;
            this.subscribeActions = new List<Action>();

            TryToConnect(null);
        }

        public event Action Connected;
        public event Action Disconnected;

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new HyperCloudException("RabbitMQ server is not connected.");
            }
            return connection.CreateModel();
        }

        public void AddSubscriptionAction(Action subscriptionAction)
        {
            subscribeActions.Add(subscriptionAction);

            try
            {
                subscriptionAction();
            }
            catch (OperationInterruptedException)
            {
                // Looks like the channel closed between our IsConnected check
                // and the subscription action. Do nothing here, when the 
                // connection comes back, the subcription action will be run then.
            }
        }

        public bool IsConnected
        {
            get { return connection != null && connection.IsOpen && !disposed; }
        }

        void StartTryToConnect()
        {
            var timer = new Timer(TryToConnect);
            timer.Change(connectAttemptIntervalMilliseconds, Timeout.Infinite);
        }

        void TryToConnect(object timer)
        {
            if (timer != null)
            {
                ((Timer)timer).Dispose();
            }

            logger.Debug("Trying to connect");
            if (disposed)
            {
                return;
            }

            try
            {
                connection = connectionFactory.CreateConnection();
                connection.ConnectionShutdown += OnConnectionShutdown;

                if (Connected != null)
                {
                    Connected();
                }
                logger.Info("Connected to RabbitMQ. Broker: '{0}', VHost: '{1}'", connectionFactory.HostName, connectionFactory.VirtualHost);

                logger.Debug("Re-creating subscribers");
                foreach (var subscribeAction in subscribeActions)
                {
                    subscribeAction();
                }
            }
            catch (BrokerUnreachableException exception)
            {
                logger.Error("Failed to connect to Broker: '{0}', VHost: '{1}'. Retrying in {2} ms\n" + 
                    "Check HostName, VirtualHost, Username and Password.", 
                    connectionFactory.HostName,
                    connectionFactory.VirtualHost,
                    connectAttemptIntervalMilliseconds,
                    exception.Message);
                StartTryToConnect();
            }
        }

        void OnConnectionShutdown(IConnection _, ShutdownEventArgs reason)
        {
            if (disposed)
            {
                return;
            }
            if (Disconnected != null)
            {
                Disconnected();
            }

            // try to reconnect and re-subscribe
            logger.Info("Disconnected from RabbitMQ Broker");
            StartTryToConnect();
        }

        private bool disposed = false;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            if (connection != null)
            {
                connection.Dispose();
            }
        }
    }
}