
using System.Text.Json;

namespace Liquid.Domain.Test
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Helper class for using the <c>LightUnitTest</c> data loading methods.
    /// </summary>
    /// <typeparam name="TUnit">The unit test class being tested. Used for referencing data file names.</typeparam>
    /// <typeparam name="TFixture">The fixture class used for setting up before testing.</typeparam>
    public class LightUnitTestCase<TUnit, TFixture> where TFixture : LightTestDisposable
    {
        protected TFixture Fixture { get; set; }

        public LightUnitTestCase(TFixture fixture)
        {
            Fixture = fixture;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TInput">The type for deserializing input.</typeparam>
        /// <typeparam name="TOutput">The type for deserializing output.</typeparam>
        /// <param name="testId"></param>
        /// <returns></returns>
        protected TestData<TInput, TOutput> LoadTestData<TInput, TOutput>(string testId)
        {
            return LightUnitTest.LoadTestData<TInput, TOutput>(typeof(TUnit).Name, testId);
        }

        protected TestData<JsonDocument, TOutput> LoadTestData<TOutput>(string testId)
        {
            return LoadTestData<JsonDocument, TOutput>(testId);
        }

        protected TestData<JsonDocument, JsonDocument> LoadTestData(string testId)
        {
            return LoadTestData<JsonDocument, JsonDocument>(testId);
        }

        protected string GetAuthorization(string role)
        {
            return LightUnitTest.GetAuthorization(role);
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}