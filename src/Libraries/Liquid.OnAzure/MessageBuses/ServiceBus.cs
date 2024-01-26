using Liquid.Base;
using Liquid.Domain;
using Liquid.Interfaces;
using Liquid.Runtime;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using System;
using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Liquid.OnAzure
{
    /// <summary>
    /// Implementation of the communication component between queues of the Azure, this class is specific to azure
    /// </summary>
    public class ServiceBus : MessageBrokerWrapper
    {
        private const int DAYS_TO_LIVE = 365;

        private ManagementClient _managementClient;
        private ServiceBusConnection _serviceBusConnection;
        private readonly ConcurrentDictionary<string, QueueClient> queues = new();
        private readonly ConcurrentDictionary<string, TopicClient> topics = new();

        private static RetryPolicy retryPolicy;
        /// <summary>
        /// The retry policy used
        /// </summary>
        public static RetryPolicy RetryPolicy
        {
            get
            {
                retryPolicy ??= new RetryExponential(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(3), 10);    
                return retryPolicy;
            }
        }

        /// <summary>
        /// Inicialize the class with set Config Name and Queue Name, must called the parent method
        /// </summary>
        /// <param name="tagConfigName"> Config Name </param>
        /// <param name="endpointName">Queue Name</param> 
        public override void Config(string tagConfigName, string endpointName)
        {
            base.Config(tagConfigName, endpointName);
            SetConnection(tagConfigName);
        }

        /// <summary>
        /// Get connection settings
        /// </summary>
        /// <param name="tagConfigName"></param>
        private void SetConnection(string tagConfigName)
        {
            MessageBrokerConfiguration config = null;
            if (string.IsNullOrEmpty(tagConfigName)) // Load specific settings if provided
                config = LightConfigurator.LoadConfig<MessageBrokerConfiguration>(nameof(ServiceBus));
            else
                config = LightConfigurator.LoadConfig<MessageBrokerConfiguration>($"{nameof(ServiceBus)}_{tagConfigName}");

            _managementClient = new(config.ConnectionString);
            _serviceBusConnection = new(config.ConnectionString);
        }

        /// <summary>
        /// Sends a message to a queue
        /// </summary>
        /// <typeparam name="T">Type of message to send</typeparam>
        /// <param name="message">Object of message to send</param>
        /// <param name="queueName">Name of the queue</param>
        /// <param name="messageLabel">Label of the message</param>
        /// <param name="minutesToLive">Message's time-to-live in minutes (default 365 days)</param>
        /// <returns>The task of Process topic</returns> 
        public override async Task SendToQueueAsync<T>(T message, string queueName = null, string messageLabel = null, int? minutesToLive = null)
        {
            var endpoint = queueName ?? EndpointName;
            QueueClient queueClient;

            if (queues.ContainsKey(endpoint))
                queues.TryGetValue(endpoint, out queueClient);
            else
            {
                queueClient = new(serviceBusConnection: _serviceBusConnection, 
                                  entityPath: endpoint, 
                                  receiveMode: ReceiveMode.PeekLock, 
                                  retryPolicy: RetryPolicy);
                queues.TryAdd(endpoint, queueClient);
            }

            var messageData = new Message(message.ToJsonBytes())
            {
                ContentType = "application/json;charset=utf-8",
                Label = messageLabel ?? typeof(T).ToString(),
                MessageId = Guid.NewGuid().ToString(),
                TimeToLive = minutesToLive is null
                                ? TimeSpan.FromDays(DAYS_TO_LIVE)
                                : TimeSpan.FromMinutes(minutesToLive.Value)
            };

            try
            {
                await queueClient.SendAsync(messageData);
            }
            catch (MessagingEntityNotFoundException)
            {
                try
                {
                    await _managementClient.CreateQueueAsync(endpoint);
                    await queueClient.SendAsync(messageData);
                }
                catch
                {
                    TrackMissedMessage(endpoint, message);
                }
            }
            catch 
            {
                TrackMissedMessage(endpoint, message);
            }
        }

        /// <summary>
        /// Sends a message to a topic
        /// </summary>
        /// <typeparam name="T">Type of message to send</typeparam>
        /// <param name="message">Object of message to send</param>
        /// <param name="topicName">Name of the topic</param>
        /// <param name="messageLabel">Label of the message</param>
        /// <param name="minutesToLive">Message's time-to-live in minutes (default 365 days)</param>
        /// <returns>The task of Process topic</returns> 
        public override async Task SendToTopicAsync<T>(T message, string topicName = null, string messageLabel = null, int? minutesToLive = null)
        {
            var endpoint = topicName ?? EndpointName;

            TopicClient topicClient;

            if (topics.ContainsKey(endpoint))
                topics.TryGetValue(endpoint, out topicClient);
            else
            {
                topicClient = new(serviceBusConnection: _serviceBusConnection, 
                                  entityPath: endpoint, 
                                  retryPolicy: RetryPolicy);
                topics.TryAdd(endpoint, topicClient);
            }

            var messageData = new Message(message.ToJsonBytes())
            {
                ContentType = "application/json;charset=utf-8",
                Label = messageLabel ?? typeof(T).ToString(),
                MessageId = Guid.NewGuid().ToString(),
                TimeToLive = minutesToLive is null
                                ? TimeSpan.FromDays(DAYS_TO_LIVE)
                                : TimeSpan.FromMinutes(minutesToLive.Value)
            };

            foreach (var kvp in message.GetUserProperties())
                messageData.UserProperties.Add(kvp.Key, kvp.Value);

            try
            {
                await topicClient.SendAsync(messageData);
            }
            catch (MessagingEntityNotFoundException)
            {
                try
                {
                    await _managementClient.CreateTopicAsync(endpoint);
                    await topicClient.SendAsync(messageData);
                }
                catch
                {
                    TrackMissedMessage(endpoint, message);
                }
            }
            catch
            {
                TrackMissedMessage(endpoint, message);
            }
        }

        private static void TrackMissedMessage<T>(string endpoint, T message) where T : ILightMessage
        {
            WorkBench.Telemetry.TrackException(new MessageMissedLightException(endpoint));

            //removes sensitive information
            JsonNode msgAsJson = message.ToJsonNode();

            msgAsJson["tokenJwt"] = null;

            WorkBench.Telemetry.TrackTrace(msgAsJson.ToJsonString(true));
        }
    }
}