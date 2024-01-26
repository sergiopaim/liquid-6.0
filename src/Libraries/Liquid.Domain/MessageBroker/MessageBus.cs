using Liquid.Domain.Test;
using Liquid.Interfaces;
using Liquid.Runtime;
using System.Threading.Tasks;

namespace Liquid.Domain
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MessageBus<T> where T : MessageBrokerWrapper, new()
    {
        private readonly T _process;

        /// <summary>
        /// Initialize the wrapper to comunicate with the message broker
        /// </summary>
        /// <param name="tagConfigName">Configuration Tag Name</param>
        /// <param name="endpointName">Queue or Topic name</param> 
        public MessageBus(string tagConfigName, string endpointName)
        {
            _process = new();
            _process.Config(tagConfigName, endpointName);
        }

        /// <summary>
        /// Sends a message to queue
        /// </summary>
        /// <typeparam name="U">type of Message</typeparam>
        /// <param name="message">Message Object</param>
        /// <param name="minutesToLive">Message time-to-live in minutes (default 365 days)</param>
        /// <returns>Task</returns>
        public Task SendToQueueAsync<U>(U message, int? minutesToLive = null) where U : ILightMessage
        {
            if (MessageBusInterceptor.ShouldInterceptMessages)
            {
                MessageBusInterceptor.Intercept(message, EndpointType.QUEUE, _process.TagConfigName, _process.EndpointName);
                return Task.FromResult(0);
            }
            return _process.SendToQueueAsync(message, minutesToLive: minutesToLive);
        }

        /// <summary>
        /// Sends a message to topic
        /// </summary>
        /// <typeparam name="U">tyep of Message</typeparam>
        /// <param name="message">Messade Object</param>
        /// <param name="minutesToLive">Message time-to-live in minutes (default 365 days)</param>
        /// <returns>Task</returns>
        public Task SendToTopicAsync<U>(U message, int? minutesToLive = null) where U : ILightMessage
        {
            if (MessageBusInterceptor.ShouldInterceptMessages)
            {
                MessageBusInterceptor.Intercept(message, EndpointType.TOPIC, _process.TagConfigName, _process.EndpointName);
                return Task.FromResult(0);
            }
            return _process.SendToTopicAsync(message, minutesToLive: minutesToLive);
        }
    }
}
