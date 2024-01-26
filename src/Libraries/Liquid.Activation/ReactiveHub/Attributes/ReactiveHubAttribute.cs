using System;

namespace Liquid.Activation
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ReactiveHubAttribute : Attribute
    {
        public string HubEndpoint{ get; }

        public ReactiveHubAttribute(string hubEndpoint = "/hub")
        {
            HubEndpoint = hubEndpoint;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
