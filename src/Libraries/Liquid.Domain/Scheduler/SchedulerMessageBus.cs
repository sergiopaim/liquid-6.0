using Liquid.Domain.Test;
using Liquid.Interfaces;
using System.Threading.Tasks;

namespace Liquid.Domain
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class SchedulerMessageBus<T> where T : MessageBrokerWrapper, new()
    {
        public static readonly string JOBS_ENDPOINT = "scheduler/jobs";
        public static readonly string COMMANDS_ENDPOINT = "scheduler/commands";

        private readonly T _process;

        private static string dispatchEndpoint = MessageBrokerWrapper.BuildNonProductionEnvironmentEndpointName(JOBS_ENDPOINT);
        private static string commandEndpoint = MessageBrokerWrapper.BuildNonProductionEnvironmentEndpointName(COMMANDS_ENDPOINT);

        public static string DispatchEndpoint { get => dispatchEndpoint; set => dispatchEndpoint = value; }
        public static string CommandEndpoint { get => commandEndpoint; set => commandEndpoint = value; }

        public SchedulerMessageBus(string schedulerName)
        {
            _process = new();
            _process.Config(schedulerName, null);
        }

        public Task SendDispatch<U>(U message) where U : ILightJobMessage
        {
            if (MessageBusInterceptor.ShouldInterceptMessages)
            {
                MessageBusInterceptor.Intercept((ILightMessage)message, EndpointType.TOPIC, _process.TagConfigName, _process.EndpointName);
                return Task.FromResult(0);
            }
            return _process.SendToTopicAsync((ILightMessage)message, topicName: DispatchEndpoint, messageLabel: message.Microservice);
        }

        public Task SendCommand<U>(U message) where U : ILightJobMessage
        {
            if (MessageBusInterceptor.ShouldInterceptMessages)
            {
                MessageBusInterceptor.Intercept((ILightMessage)message, EndpointType.QUEUE, _process.TagConfigName, _process.EndpointName);
                return Task.FromResult(0);
            }

            var result = _process.SendToQueueAsync((ILightMessage)message, queueName: CommandEndpoint, messageLabel: message.Microservice);

            return result;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
