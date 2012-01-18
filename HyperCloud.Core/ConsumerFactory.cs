using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

using System.Threading;
using System.Threading.Tasks;

using System.IO;
using System.Text;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing.v0_9_1;
using RabbitMQ.Util;

namespace HyperCloud
{
    public delegate Task ConsumerCallback(BasicDeliverEventArgs basicDeliverEventArgs);

    public class ConsumerFactory : IDisposable
    {
        private readonly ILogger logger;

        private SharedQueue sharedQueue = new SharedQueue();

        private readonly IDictionary<string, SubscriptionInfo> subscriptions = new ConcurrentDictionary<string, SubscriptionInfo>();
        
        private readonly object sharedQueueLock = new object();
        private readonly Thread subscriptionCallbackThread;

        public ConsumerFactory(ILogger logger)
        {
            this.logger = logger;

            subscriptionCallbackThread = new Thread(_ =>
            {
                while (true)
                {
                    if (disposed) 
                    {
                        break;
                    }

                    try
                    {
                        BasicDeliverEventArgs deliverEventArgs;
                        lock (sharedQueueLock)
                        {
                            deliverEventArgs = (BasicDeliverEventArgs)sharedQueue.Dequeue();
                        }
                        if (deliverEventArgs != null)
                        {
                            HandleMessageDelivery(deliverEventArgs);
                        }
                    }
                    catch (EndOfStreamException)
                    {
                        Thread.Sleep(10);
                    }
                }
            });
            subscriptionCallbackThread.Start();
        }

        private void HandleMessageDelivery(BasicDeliverEventArgs basicDeliverEventArgs)
        {
            var consumerTag = basicDeliverEventArgs.ConsumerTag;
            if (!subscriptions.ContainsKey(consumerTag))
            {
                throw new HyperCloudException("No callback found for ConsumerTag {0}", consumerTag);
            }

            logger.Debug("Subscriber Recieved {0}, CorrelationId {1}", 
                basicDeliverEventArgs.RoutingKey, basicDeliverEventArgs.BasicProperties.CorrelationId);

            var subscriptionInfo = subscriptions[consumerTag];

            try
            {
                var completionTask = subscriptionInfo.Callback(basicDeliverEventArgs);
                completionTask.ContinueWith(task =>
                {
                    logger.Debug("In handle message. Task after process lambda. ThreadID: {0}", Thread.CurrentThread.ManagedThreadId);
                    if (task.IsFaulted)
                    {
                        var exception = task.Exception;
                        logger.Error(BuildErrorMessage(basicDeliverEventArgs, exception));
                    }
                    DoAck(basicDeliverEventArgs, subscriptionInfo);
                });
            }
            catch (Exception exception)
            {
                logger.Error(BuildErrorMessage(basicDeliverEventArgs, exception));
                DoAck(basicDeliverEventArgs, subscriptionInfo);
            }
        }

        private void DoAck(BasicDeliverEventArgs basicDeliverEventArgs, SubscriptionInfo subscriptionInfo)
        {
            const string failedToAckMessage = "Basic ack failed because chanel was closed with message {0}." +
                                              " Message remains on RabbitMQ and will be retried.";

            try
            {
                subscriptionInfo.Consumer.Model.BasicAck(basicDeliverEventArgs.DeliveryTag, false);
            }
            catch (AlreadyClosedException alreadyClosedException)
            {
                logger.Info(failedToAckMessage, alreadyClosedException.Message);
            }
            catch (IOException ioException)
            {
                logger.Info(failedToAckMessage, ioException.Message);
            }
        }

        private string BuildErrorMessage(BasicDeliverEventArgs basicDeliverEventArgs, Exception exception)
        {
            var message = Encoding.UTF8.GetString(basicDeliverEventArgs.Body);

            var properties = basicDeliverEventArgs.BasicProperties as BasicProperties;
            var propertiesMessage = new StringBuilder();
            if (properties != null)
            {
                properties.AppendPropertyDebugStringTo(propertiesMessage);
            }

            return "Exception thrown by subscription calback.\n" +
                   string.Format("\tExchange:    '{0}'\n", basicDeliverEventArgs.Exchange) +
                   string.Format("\tRouting Key: '{0}'\n", basicDeliverEventArgs.RoutingKey) +
                   string.Format("\tRedelivered: '{0}'\n", basicDeliverEventArgs.Redelivered) +
                   string.Format("Message:\n{0}\n", message) +
                   string.Format("BasicProperties:\n{0}\n", propertiesMessage) +
                   string.Format("Exception:\n{0}\n", exception);
        }

        public DefaultBasicConsumer CreateConsumer(IModel model, ConsumerCallback callback)
        {
            var consumer = new QueueingBasicConsumer(model, sharedQueue);
            var consumerTag = Guid.NewGuid().ToString();
            consumer.ConsumerTag = consumerTag;
            subscriptions.Add(consumerTag, new SubscriptionInfo(consumer, callback));
            return consumer;
        }

        public void ClearConsumers()
        {
            sharedQueue.Close();

            lock (sharedQueueLock)
            {
                logger.Debug("Clearing consumer subscriptions");
                sharedQueue = new SharedQueue();
                subscriptions.Clear();
            }
        }

        private bool disposed = false;

        public void Dispose()
        {
            if (disposed) 
            {
                return;
            }
            sharedQueue.Close();
            disposed = true;
        }
    }

    public class SubscriptionInfo
    {
        public IBasicConsumer Consumer { get; private set; }
        public ConsumerCallback Callback { get; private set; }

        public SubscriptionInfo(IBasicConsumer consumer, ConsumerCallback callback)
        {
            Consumer = consumer;
            Callback = callback;
        }
    }
}