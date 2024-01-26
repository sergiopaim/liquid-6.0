using System;

namespace Liquid.Activation
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SchedulerAttribute : Attribute
    {
        public string SchedulerName { get; }
        public string SubscriptionName { get; }
        public int MaxConcurrentCalls { get; }

        public SchedulerAttribute(string schedulerName, string subscriptionName, int maxConcurrentCalls = 10)
        {
            SchedulerName = schedulerName;
            SubscriptionName = subscriptionName;
            MaxConcurrentCalls = maxConcurrentCalls;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
