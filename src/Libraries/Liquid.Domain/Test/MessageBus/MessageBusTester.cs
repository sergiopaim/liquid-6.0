using Liquid.Base;
using Liquid.Domain.API;
using System;
using System.Text.Json;

namespace Liquid.Domain.Test
{
    /// <summary>
    /// Helper class to test messageBus workers
    /// </summary>
    public class MessageBusTester
    {
        private readonly InterceptedMessageDictionary interceptedMessages;
        private readonly ApiWrapper api;

        /// <summary>
        /// Messages that were intercepted for the current session (OperationId)
        /// </summary>
        public InterceptedMessageDictionary InterceptedMessages { get => interceptedMessages; }

        /// <summary>
        /// Instanciates a MessageBus tester 
        /// </summary>
        /// <param name="api">the API pointing to the respective microservice to be tested</param>
        public MessageBusTester(ApiWrapper api)
        {
            this.api = api;
            interceptedMessages = new InterceptedMessageDictionary(api);
        }

        /// <summary>
        /// Sends a json message to a queue
        /// </summary>
        /// <param name="queueName">The name of the queue</param>
        /// <param name="message">The message json data</param>
        /// <returns>A domain response</returns>
        public HttpResponseMessageWrapper<DomainResponse> SendToQueue(string queueName, JsonDocument message)
        {
            return api.Post<DomainResponse>($"messageBus/send/queue/{queueName}", message);
        }

        /// <summary>
        /// Sends a json message to a topic
        /// </summary>
        /// <param name="topic">The name of the topic</param>
        /// <param name="message">The message json data</param>
        /// <returns>A domain response</returns>
        public HttpResponseMessageWrapper<DomainResponse> SendToTopic(string topic, JsonDocument message)
        {
            return api.Post<DomainResponse>($"messageBus/send/topic/{topic}", message);
        }

        /// <summary>
        /// Sends a message to a queue
        /// </summary>
        /// <typeparam name="T">The type of the message</typeparam>
        /// <param name="queueName">The name of the queue</param>
        /// <param name="message">The message data</param>
        /// <returns>A domain response</returns>
        public HttpResponseMessageWrapper<DomainResponse> SendToQueue<T>(string queueName, T message)
        {
            return SendToQueue(queueName, message.ToJsonDocument());
        }

        /// <summary>
        /// Sends a message to a topic
        /// </summary>
        /// <typeparam name="T">The type of the message</typeparam>
        /// <param name="topicName">The name of the topic</param>
        /// <param name="message">The message data</param>
        /// <returns>A domain response</returns>
        public HttpResponseMessageWrapper<DomainResponse> SendToTopic<T>(string topicName, T message)
        {
            return SendToTopic(topicName, message.ToJsonDocument());
        }
    }
}