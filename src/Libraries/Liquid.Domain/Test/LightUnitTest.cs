using Liquid.Base;
using Liquid.Runtime;
using System.Text.Json;

namespace Liquid.Domain.Test
{
    /// <summary>
    /// Provides helper methods for setting up and running unit tests using available configuration files.
    /// </summary>
    public static class LightUnitTest
    {
        /// <summary>
        /// The environment the tests are being run on.
        /// Ex.: <c>"Development"</c>
        /// </summary>
        private static string _environmentName;

        /// <summary>
        /// Sets up the framework's workbench with info from the provided settings file.
        /// </summary>
        /// <param name="settingsFileName">The base name for the setting file.</param>
        /// <param name="environmentName"> The name for the environment the tests are going to be run on.
        /// Used as suffix for the file name. Ex.: With a settings file <c>"runsettings"</c> and
        /// environment name <c>"Development"</c>, settings file used will be <c>"appsettings.Development.json"</c>.
        /// </param>
        public static void PrepareUnitTestMode(string settingsFileName = "runsettings", string environmentName = "Development")
        {
            _environmentName = environmentName;
            WorkBench.PrepareUnitTestMode(settingsFileName, environmentName);
        }

        /// <summary>
        /// Loads JSON data from file bundled with the project.
        /// </summary>
        /// <param name="fileName">The name of the file to be read.</param>
        /// <param name="subDirectory">The subdirectory path to the file.</param>
        /// <param name="withEnvironmentName">If true, file name will be suffixed with environment name.</param>
        /// <returns>The JSON data from the specified file.</returns>
        private static JsonDocument LoadData(string fileName, string subDirectory = "", bool withEnvironmentName = false)
        {
            var environmentName = withEnvironmentName ?
                _environmentName : "";
            return StubHandler.GetStubData<JsonDocument>(fileName, environmentName, subDirectory);
        }

        /// <summary>
        /// Loads JSON data from file bundled with the project, with specified input and output data types.
        /// </summary>
        /// <typeparam name="TInput">The type for deserializing input.</typeparam>
        /// <typeparam name="TOutput">The type for deserializing output.</typeparam>
        /// <param name="unitName">The name of the unit to be tested.</param>
        /// <param name="inputId">The top-level key inside the JSON to be retrieved</param>
        /// <param name="withEnvironmentName">If true, file name will be suffixed with environment name.</param>
        /// <returns>The JSON data from the specified file.</returns>
        /// 
        public static TestData<TInput, TOutput> LoadTestData<TInput, TOutput>(string unitName, string inputId, bool withEnvironmentName = false)
        {
            var testData = LoadData(unitName, "Data", withEnvironmentName) 
                              ?? throw new LightException($"Failed to load test file '..\\Data\\{unitName}.json'\n" +
                                                          $"CHECK if its property 'Copy to Output Directory' is set to 'Copy if newer'");
            try
            {                
                return testData.Property(inputId).ToObject<TestData<TInput, TOutput>>();
            }

            catch
            {
                throw new LightException($"Failed to retrieve test data entry with id {inputId} from test file for component {unitName}.\n" +
                                         $"CHECK json structure: {testData.Property(inputId).ToJsonString(true)}");
            }
        }

        /// <summary>
        /// Loads test data as object format
        /// </summary>
        /// <param name="unitName"></param>
        /// <param name="inputId"></param>
        /// <param name="withEnvironmentName"></param>
        /// <returns></returns>
        public static TestData<JsonDocument, JsonDocument> LoadTestData(string unitName, string inputId, bool withEnvironmentName = false)
        {
            return LoadTestData<JsonDocument, JsonDocument>(unitName, inputId, withEnvironmentName);
        }

        /// <summary>
        /// Loads test data as informed types
        /// </summary>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="unitName"></param>
        /// <param name="inputId"></param>
        /// <param name="withEnvironmentName"></param>
        /// <returns></returns>
        public static TestData<JsonDocument, TOutput> LoadTestData<TOutput>(string unitName, string inputId, bool withEnvironmentName = false)
        {
            return LoadTestData<JsonDocument, TOutput>(unitName, inputId, withEnvironmentName);
        }

        /// <summary>
        /// Load authorization token for the specified role.
        /// The data is loaded from the <c>"authorizations.json"</c> file, located at the project root.
        /// </summary>
        /// <param name="role">The role for specifying the type of token to retrieve.</param>
        /// <returns></returns>
        public static string GetAuthorization(string role)
        {
            // will only be run under development/integration environments
            var authData = LoadData("authorizations");

            try
            {
                return authData.Property(role).AsString();
            }
            catch
            {
                return default;
            }
        }

    }
}
