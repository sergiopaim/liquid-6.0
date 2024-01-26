using Liquid.Interfaces;

namespace Liquid.Domain.Test
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Used for easily deserializing messages, since `GenericInterceptedMessage` uses interfaces
    /// </summary>
    /// <typeparam name="TLightMessage"></typeparam>
    public class InterceptedMessage<TLightMessage> where TLightMessage : ILightMessage
    {
        public TLightMessage Message { get; set; }
        public EndpointType EntityType { get; set; }
        public string TagConfigName { get; set; }
        public string ChannelName { get; set; }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
