using Liquid.Base;
using Liquid.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Liquid.Domain.Test
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static class MessageBusInterceptor
    {
        // Intercept by default in dev environment
        public static bool InterceptMessages { get; set; } = Environment.GetEnvironmentVariable("INTERCEPT_MESSAGES") == "true";
        public static bool ShouldInterceptMessages =>
            (WorkBench.IsDevelopmentEnvironment || WorkBench.IsIntegrationEnvironment) && InterceptMessages;

        // thred safe
        public static ConcurrentDictionary<string, ConcurrentBag<GenericInterceptedMessage>> InterceptedMessages { get; } = new();
        public static List<GenericInterceptedMessage> InterceptedMessagesByOperationId(string operationId) =>
            InterceptedMessages.ContainsKey(operationId) ? InterceptedMessages[operationId].ToList() : new List<GenericInterceptedMessage>();

        public static List<GenericInterceptedMessage> InterceptedMessagesByOperationIdAndMessageType(string operationId, string messageType)
        {
            var messages = InterceptedMessagesByOperationId(operationId);

            return messages.Where(m => m.MessageType == messageType).ToList();
        }

        public static void Intercept(ILightMessage message, EndpointType endpointType, string tagConfigName, string channelName)
        {
            if (!InterceptedMessages.ContainsKey(message?.OperationId))
                InterceptedMessages.TryAdd(message?.OperationId, new ConcurrentBag<GenericInterceptedMessage>());
            var interceptedMessage = new GenericInterceptedMessage
            {
                Message = message.ToJsonDocument(),
                MessageType = message.GetType().Name,
                EndpointType = endpointType,
                TagConfigName = tagConfigName,
                ChannelName = channelName
            };
            InterceptedMessages[message?.OperationId].Add(interceptedMessage);
        }

        public static void ClearMessages(string operationId = null)
        {
            if (string.IsNullOrEmpty(operationId))
                InterceptedMessages.Clear();
            else if (InterceptedMessages.ContainsKey(operationId))
                InterceptedMessages[operationId].Clear();
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
