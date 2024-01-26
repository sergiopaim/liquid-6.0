using System.Text.Json;

namespace Liquid.Domain.Test
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public enum EndpointType {
        QUEUE, TOPIC
    }

    public class GenericInterceptedMessage
    {
        public JsonDocument Message { get; set; }
        public string MessageType { get; set; }
        public EndpointType EndpointType { get; set; }
        public string TagConfigName { get; set; }
        public string ChannelName { get; set; }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
