using Liquid.Domain;
using System;

namespace Liquid.Activation
{
    /// <summary>
    /// Attribute used for connect a Queue.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class QueueAttribute : Attribute
    {
        /// <summary>
        /// Get a Queue Name.
        /// </summary>
        public virtual string QueueName { get; }

        /// <summary>
        /// Take Quantity
        /// </summary>
        public virtual int MaxConcurrentCalls { get; }

        /// <summary>
        /// Delete after Read
        /// </summary>
        public virtual bool DeleteAfterRead { get; }

        /// <summary>
        /// Constructor used to inform a Queue name.
        /// </summary>
        /// <param name="queueName">Queue Name</param>
        /// <param name="maxConcurrentCalls">Quantity to take in unit process, by default 10</param>
        /// <param name="deleteAfterRead">Delete after read the message? by default true</param>
        public QueueAttribute(string queueName, int maxConcurrentCalls = 10, bool deleteAfterRead = true) {
            QueueName = MessageBrokerWrapper.BuildNonProductionEnvironmentEndpointName(queueName);
            MaxConcurrentCalls = maxConcurrentCalls;
            DeleteAfterRead = deleteAfterRead;
        }
    }
}
