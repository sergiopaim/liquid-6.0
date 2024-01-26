using Liquid.Base;
using Liquid.Interfaces;
using System;
using System.Threading.Tasks;

namespace Liquid.Domain
{
    /// <summary>
    /// Wrapper class to instatiate a message broker implementation
    /// Helping to enable a smooth integration
    /// </summary>
    public class MessageBrokerWrapper : IMessageBrokerWrapper
    {
        private string _endpointName;

        /// <summary>
        /// The name of the tag configuration name
        /// </summary>
        public string TagConfigName { get; private set; }

        /// <summary>
        /// The name of the endpoint
        /// </summary>
        public string EndpointName { 
            get => _endpointName; 
            protected set 
            {
                _endpointName = BuildNonProductionEnvironmentEndpointName(value);
            } 
        }

        /// <summary>
        /// Config the class with set Config Name, Endpoint Name  
        /// </summary>
        /// <param name="tagConfigName">Config name</param>
        /// <param name="endpointName">Endpoint name</param> 
        public virtual void Config(string tagConfigName, string endpointName)
        {          
            TagConfigName = tagConfigName;
            EndpointName = endpointName;
        }

        /// <summary>
        /// Initialize the message wrapper
        /// </summary>
        public virtual void Initialize()
        {
        }

        /// <summary>
        /// Builds the endpoint for a test environment
        /// </summary>
        /// <param name="endpointName">Endpoint name</param> 
        /// <returns></returns>
        public static string BuildNonProductionEnvironmentEndpointName(string endpointName)
        {
            if (string.IsNullOrEmpty(endpointName))
                return null;

            if (WorkBench.IsDevelopmentEnvironment)
            {
                var mn = Environment.MachineName;
                if (mn.Length > 7)
                    // assuming MachineName of the format `DESKTOP-XXXXXXX`
                    mn = mn[^7..];

                endpointName = $"{mn}-{endpointName}";
            }
            else if (WorkBench.IsIntegrationEnvironment)
                endpointName = $"int-{endpointName}";
            else if (WorkBench.IsQualityEnvironment)
                endpointName = $"qa-{endpointName}";
            else if (WorkBench.IsDemonstrationEnvironment)
                endpointName = $"demo-{endpointName}";

            return endpointName;
        }

        /// <summary>
        /// Send a message to a queue
        /// </summary>
        /// <typeparam name="T">Type of message</typeparam>
        /// <param name="message">Message contents</param>
        /// <param name="queueName">Name of the queue</param>
        /// <param name="messageLabel">Label of the message</param>
        /// <param name="minutesToLive">Message's time-to-live in minutes (default 365 days)</param>
        /// <returns>A Task that completes when the middleware has completed processing.</returns>
        public virtual Task SendToQueueAsync<T>(T message, string queueName = null, string messageLabel = null, int? minutesToLive = null) where T : ILightMessage { return Task.FromResult(0); }

        /// <summary>
        /// Send a message to a topic
        /// </summary>
        /// <typeparam name="T">Type of message</typeparam>
        /// <param name="message">Message contents</param>
        /// <param name="topicName">Name of the queue</param>
        /// <param name="messageLabel">Label of the message</param>
        /// <param name="minutesToLive">Message's time-to-live in minutes (default 365 days)</param>
        /// <returns>A Task that completes when the middleware has completed processing.</returns>
        public virtual Task SendToTopicAsync<T>(T message, string topicName = null, string messageLabel = null, int? minutesToLive = null) where T : ILightMessage { return Task.FromResult(0); }

        /// <summary>
        /// HealthCheck
        /// </summary>
        /// <param name="serviceKey"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LightHealth.HealthCheckStatus HealthCheck(string serviceKey, string value)
        {
            try
            {
                return LightHealth.HealthCheckStatus.Healthy;
            }
            catch
            {
                return LightHealth.HealthCheckStatus.Unhealthy;
            }
        }
    }
}
