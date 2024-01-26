using System;

namespace Liquid.Runtime
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
    public class SwaggerIgnoreAttribute : Attribute
    {
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
