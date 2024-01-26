using Microsoft.IdentityModel.Tokens;
using System;
using System.Reflection;
using Xunit.Sdk;

namespace Liquid.Domain.Test
{
    /// <summary>
    /// Sets the test case method to run at a specific DateTime
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RunAtDateTimeAttribute: BeforeAfterTestAttribute
    {
        private readonly DateTime at;
        /// <summary>
        /// Sets the test case method to run at a specific DateTime
        /// </summary>
        /// <param name="utcValue">The date and time in UTC format</param>
        public RunAtDateTimeAttribute(string utcValue)
        {
            if (DateTime.TryParse(utcValue, out DateTime utc))
                at = utc;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Before(MethodInfo methodUnderTest)
        {
            WorkBench.UtcNow = at;
        }

        public override void After(MethodInfo methodUnderTest)
        {
            WorkBench.UtcNow = default;
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}