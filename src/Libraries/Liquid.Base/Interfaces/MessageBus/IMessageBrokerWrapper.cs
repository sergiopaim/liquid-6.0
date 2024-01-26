using System.Threading.Tasks;

namespace Liquid.Interfaces
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Programming interface that applications can use to control brokers
    /// </summary>
    public interface IMessageBrokerWrapper : IWorkBenchHealthCheck
    {
        void Config(string tagConfigName, string endpointName);
        Task SendToQueueAsync<T>(T message, string queueName = null, string messageLabel = null, int? minutesToLive = null) where T : ILightMessage;
        Task SendToTopicAsync<T>(T message, string topicName = null, string messageLabel = null, int? minutesToLive = null) where T : ILightMessage;
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
