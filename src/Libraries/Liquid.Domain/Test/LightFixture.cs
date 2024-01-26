
using Liquid.Domain.API;
using System;

namespace Liquid.Domain.Test
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Fixture class to use with LightUnitTest.
    /// </summary>
    public class LightFixture : LightTestDisposable
    {
        /// <summary>
        /// The reference to the API being tested
        /// </summary>
        public ApiWrapper Api { get; private set; }

        /// <summary>
        /// The reference to the MessageBus workers being tested and intercepted messages
        /// </summary>
        public MessageBusTester MessageBus { get; private set; }

        /// <summary>
        /// The reference to the Scheduler jobs being tested
        /// </summary>
        public SchedulerTester Scheduler { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiName">The name of API the testing microservice provides.</param>
        /// <param name="authTokenName">The name of the standard authToken authorization to use on the tests</param>
        public LightFixture(string apiName, string authTokenName)
        {
            LightUnitTest.PrepareUnitTestMode("runsettings", WorkBench.EnvironmentName);
            
            Api = new(apiName, LightUnitTest.GetAuthorization(authTokenName));
            
            Api.Put("forceStub/enable");
            
            Api.Put("reseed/Unit");

            MessageBus = new(Api);

            Scheduler = new(MessageBus);
        }

        public override void Dispose()
        {
            Api.Put("forceStub/disable");

            if (WorkBench.IsIntegrationEnvironment)
                Api.Put("reseed/Integration");

            MessageBus.InterceptedMessages.Clear();

            GC.SuppressFinalize(this);
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
