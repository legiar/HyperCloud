using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using System.Threading;
using System.Threading.Tasks;

using System.Text;

using RabbitMQ.Client;

using System.Configuration;

namespace HyperCloud
{
    public class SubscribeOptions
    {
        public bool Durable { get; set; }
        public bool AutoDelete { get; set; }
        public bool Exclusive { get; set; }

        public SubscribeOptions()
        {
            Durable = true;
            AutoDelete = false;
            Exclusive = false;
        }
    }

    public class Bus : IBus
    {
        private readonly Configuration.BusSection config;
        private readonly IPersistentConnection connection;
        private readonly ILogger logger;
        private readonly ConsumerFactory consumerFactory;
        private readonly ISerializer serializer = new Serializer.JsonSerializer();
        private readonly List<IModel> channels = new List<IModel>();
        private const bool noAck = false;
        private const int prefetchCount = 1;

        public bool durableSubscription = true;
        public bool autodeleteSubscription = false;

        public static Bus Create(ILogger logger = null)
        {
            var busConnectionString = ConfigurationManager.ConnectionStrings["bus"];
            if (busConnectionString == null)
            {
                throw new HyperCloudException(
                    "Could not find a connection string for RabbitMQ. " +
                    "Please add a connection string in the <ConnectionStrings> secion" +
                    "of the application's configuration file. For example: " +
                    "<add name=\"bus\" connectionString=\"localhost\" />");
            }
            if (logger == null)
            {
                return Create(busConnectionString.ConnectionString);
            }
            else
            {
                return Create(busConnectionString.ConnectionString, logger);
            }
        }

        public static Bus Create(string connectionString)
        {
            ILogger logger = null;

            if (Environment.UserInteractive)
            {
                logger = new Loggers.ConsoleLogger();
            }
            else
            {
                logger = new Loggers.EmptyLogger();
            }

            return Create(connectionString, logger);
        }

        public static Bus Create(string connectionString, ILogger logger)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException("connectionString");
            }

            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            var connectionValues = new ConnectionString(connectionString);
            ConnectionFactory connectionFactory = new ConnectionFactory();
            connectionFactory.HostName = connectionValues.Host;
            connectionFactory.Port = connectionValues.Port;
            connectionFactory.UserName = connectionValues.UserName;
            connectionFactory.Password = connectionValues.Password;
            connectionFactory.Ssl.Enabled = connectionValues.Ssl;
            connectionFactory.RequestedHeartbeat = 50;
            if (connectionFactory.Ssl.Enabled)
            {
                connectionFactory.Ssl.ServerName = connectionValues.Host;
                connectionFactory.Ssl.Version = System.Security.Authentication.SslProtocols.Tls;
            }
            return new Bus(connectionFactory, logger);
        }

        private Bus(ConnectionFactory connectionFactory, ILogger logger)
        {
            System.Configuration.Configuration root_config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var hyperadmin_config = root_config.SectionGroups["hyperadmin"];
            if (hyperadmin_config != null)
            {
                config = (hyperadmin_config as Configuration.HyperCloudGroup).BusConfigurationSection as Configuration.BusSection;
            }

            if (connectionFactory == null)
            {
                throw new ArgumentNullException("connectionFactory");
            }

            this.logger = logger;
            this.consumerFactory = new ConsumerFactory(logger);

            connection = new PersistentConnection(connectionFactory, logger);
            connection.Connected += OnConnected;
            connection.Disconnected += consumerFactory.ClearConsumers;
            connection.Disconnected += OnDisconnected;
        }

        // channels should not be shared between threads.
        private ThreadLocal<IModel> threadLocalChannel = new ThreadLocal<IModel>();
        private IModel LocalChannel
        {
            get
            {
                if (!threadLocalChannel.IsValueCreated)
                {
                    threadLocalChannel.Value = connection.CreateModel();
                    channels.Add(threadLocalChannel.Value);
                }
                return threadLocalChannel.Value;
            }
        }

        private string GetMessageType<TMessage>()
        {
            Type type = typeof(TMessage);
            foreach (object attribute in type.GetCustomAttributes(true))
            {
                if (attribute is MessageTypeAttribute)
                {
                    return (attribute as MessageTypeAttribute).Type;
                }
            }
            return type.Name;
        }

        private ISerializer GetMessageSerializer<TMessage>()
        {
            Type type = typeof(TMessage);
            foreach (object attribute in type.GetCustomAttributes(true))
            {
                if (attribute is MessageSerializerAttribute)
                {
                    return (attribute as MessageSerializerAttribute).Serializer;
                }
            }
            return serializer;
        }

        private void CheckMessageType<TMessage>(IBasicProperties properties)
        {
            var messageType = GetMessageType<TMessage>();
            if (properties.Type != messageType)
            {
                logger.Error("Message type is incorrect. Expected '{0}', but was '{1}'", messageType, properties.Type);
                throw new HyperAdminInvalidMessageTypeException("Message type is incorrect. Expected '{0}', but was '{1}'", messageType, properties.Type);
            }
        }

        public void DeclareExchange(string exchange, string type = "")
        {
            if (!connection.IsConnected)
            {
                throw new HyperCloudException("Publish failed. No rabbit server connected.");
            }
            try
            {
                if (type == "")
                {
                    type = ExchangeType.Direct;
                }
                LocalChannel.ExchangeDeclare(exchange, type, true, false, null);
            }
            catch (RabbitMQ.Client.Exceptions.OperationInterruptedException exception)
            {
                throw new HyperCloudException("Declare channel failed: '{0}'", exception.Message);
            }
            catch (System.IO.IOException exception)
            {
                throw new HyperCloudException("Declare channel failed: '{0}'", exception.Message);
            }
        }

        public void Publish<T>(T message, string address = "")
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }
            if (!connection.IsConnected)
            {
                throw new HyperCloudException("Publish failed. No rabbit server connected.");
            }

            string messageType = GetMessageType<T>();
            var publication = PublicationAddress.Parse(address);
            if (publication == null)
            {
                publication = new PublicationAddress(ExchangeType.Direct, messageType, messageType);
            }
            DeclareExchange(publication.ExchangeName, publication.ExchangeType);

            try
            {
                var properties = LocalChannel.CreateBasicProperties();
                properties.SetPersistent(true);
                properties.Type = messageType;
                properties.CorrelationId = Guid.NewGuid().ToString();
                properties.Timestamp = new AmqpTimestamp(System.DateTime.Now.ToFileTimeUtc());

                IMessageHeader message_headers = message as IMessageHeader;
                if (message_headers != null)
                {
                    properties.MessageId = message_headers.MessageId;
                    properties.Headers = message_headers.Headers;
                }

                var messageSerialize = GetMessageSerializer<T>();
                properties.ContentType = messageSerialize.ContentType;
                var body = messageSerialize.MessageToBytes(message);
                LocalChannel.BasicPublish(publication.ExchangeName, publication.RoutingKey, properties, body);

                logger.Debug("Published {0}, CorrelationId {1}", messageType, properties.CorrelationId);
            }
            catch (RabbitMQ.Client.Exceptions.OperationInterruptedException exception)
            {
                throw new HyperCloudException("Publish Failed: '{0}'", exception.Message);
            }
            catch (System.IO.IOException exception)
            {
                throw new HyperCloudException("Publish Failed: '{0}'", exception.Message);
            }
        }

        public void Subscribe<T>(string subscription, Action<T> onMessage, SubscribeOptions options = null)
        {
            SubscribeAsync<T>(subscription, msg =>
            {
                var tcs = new TaskCompletionSource<object>();
                try
                {
                    onMessage(msg);
                    tcs.SetResult(null);
                }
                catch (Exception exception)
                {
                    tcs.SetException(exception);
                }
                return tcs.Task;
            }, options);
        }

        public void SubscribeAsync<T>(string subscription, Func<T, Task> onMessage, SubscribeOptions options = null)
        {
            if (onMessage == null)
            {
                throw new ArgumentNullException("onMessage");
            }
            if (options == null)
            {
                options = new SubscribeOptions()
                {
                    Durable = durableSubscription,
                    AutoDelete = autodeleteSubscription
                };
            }
            string messageType = typeof(T).Name;
            var publication = PublicationAddress.Parse(subscription);
            if (publication == null)
            {
                publication = new PublicationAddress(ExchangeType.Direct, messageType, messageType);
            }
            var subscriptionQueue = string.Format("{0}-{1}", publication.ExchangeName, publication.RoutingKey);
            if (options.Exclusive)
            {
                subscriptionQueue = string.Format("{0}-{1}", subscriptionQueue, Guid.NewGuid().ToString());
            }

            Action subscribeAction = () =>
            {
                if (!IsConnected)
                {
                    // do nothing - after connection established, all subscribe action will be restarted
                    return;
                }
                var channel = connection.CreateModel();
                channels.Add(channel);
                DeclareExchange(publication.ExchangeName, publication.ExchangeType);

                channel.BasicQos(0, prefetchCount, false);
                var queue = channel.QueueDeclare(
                    subscriptionQueue,  // queue
                    options.Durable,    // durable
                    options.Exclusive,  // exclusive
                    options.AutoDelete, // autoDelete
                    null);              // arguments

                channel.QueueBind(queue, publication.ExchangeName, publication.RoutingKey);

                var consumer = consumerFactory.CreateConsumer(channel,
                    (basicDeliverEventArgs) =>
                    {
                        CheckMessageType<T>(basicDeliverEventArgs.BasicProperties);
                        var messageSerialize = GetMessageSerializer<T>();
                        // TODO: Check message content-type
                        var message = messageSerialize.BytesToMessage<T>(basicDeliverEventArgs.Body);
                        IMessageHeader message_headers = message as IMessageHeader;
                        if (message_headers != null)
                        {
                            if (basicDeliverEventArgs.BasicProperties.MessageId != null)
                            {
                                message_headers.MessageId = basicDeliverEventArgs.BasicProperties.MessageId;
                            }
                            message_headers.Headers = basicDeliverEventArgs.BasicProperties.Headers;
                        }
                        return onMessage(message);
                    });

                channel.BasicConsume(subscriptionQueue, noAck, consumer.ConsumerTag, consumer);
            };
            connection.AddSubscriptionAction(subscribeAction);
        }

        public event Action Connected;

        protected void OnConnected()
        {
            if (Connected != null)
            {
                Connected();
            }
        }

        public event Action Disconnected;

        protected void OnDisconnected()
        {
            threadLocalChannel.Dispose();
            threadLocalChannel = new ThreadLocal<IModel>();

            if (Disconnected != null)
            {
                Disconnected();
            }
        }

        public bool IsConnected
        {
            get { return connection.IsConnected; }
        }

        private bool disposed = false;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            // Abort all channels
            if (channels.Count > 0) {
                foreach (var channel in channels) {
                    if (channel != null)
                    {
                        channel.Abort();
                    }
                }
            }
            threadLocalChannel.Dispose();
            consumerFactory.Dispose();
            connection.Dispose();

            disposed = true;
        }
    }
}