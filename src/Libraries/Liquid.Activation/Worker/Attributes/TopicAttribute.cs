using Liquid.Domain;
using System;
using System.Linq.Expressions;

namespace Liquid.Activation
{
    /// <summary>
    /// Attribute used for connect a Topic and Subscription.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class TopicAttribute : Attribute
    {
        /// <summary>
        /// Topic Name
        /// </summary>
        public virtual string TopicName { get; }

        /// <summary>
        /// Subscription Name
        /// </summary>
        public virtual string Subscription { get; }

        /// <summary>
        /// SQL Filter string
        /// </summary>
        public virtual string SqlFilter { get; }

        /// <summary>
        /// Take Quantity
        /// </summary>
        public virtual int MaxConcurrentCalls { get; }

        /// <summary>
        /// Delete after Read
        /// </summary>
        public virtual bool DeleteAfterRead { get; }

        /// <summary>
        /// Constructor used to inform a Topic, Subscription name and Sql Filter.
        /// </summary>
        /// <param name="topicName">Topic Name</param>
        /// <param name="subscriberName">Subscription Name</param>
        /// <param name="maxConcurrentCalls">Number of concurrent calls the MS could do to the bus</param>
        /// <param name="deleteAfterRead">True if the message should be deleted after pickedup</param>
        /// <param name="sqlFilter">SQL Filter</param>
        public TopicAttribute(string topicName, string subscriberName, int maxConcurrentCalls = 10, bool deleteAfterRead = true, string sqlFilter = "")
        {
            TopicName = MessageBrokerWrapper.BuildNonProductionEnvironmentEndpointName(topicName);
            Subscription = subscriberName;
            SqlFilter = sqlFilter;
            MaxConcurrentCalls = maxConcurrentCalls;
            DeleteAfterRead = deleteAfterRead;
        }
    }
}