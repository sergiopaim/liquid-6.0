using Liquid.Base;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Liquid.Runtime
{
#pragma warning disable IDE0044 // Add readonly modifier
    /// <summary>
    /// Validates the Section Property from LightAPI
    /// </summary>
    public static class LightConfigurator
    {
        private static JsonDocument _secrets;
        private static int secretsInitiated;
        private static JsonDocument Secrets
        {
            get
            {
                Interlocked.Increment(ref secretsInitiated);
                if (secretsInitiated == 1)
                {
                    _secrets = LoadSecrets();
                }
                return _secrets;
            }
        }

        private static JsonDocument _generalConfig = LoadGeneralConfig();

        private static JsonDocument LoadSecrets()
        {
            bool forceToTestOrDebug = false;

            if (!WorkBench.IsDevelopmentEnvironment || forceToTestOrDebug)
            {
                try
                {
                    string json = File.ReadAllText("/k8s/secrets/server-secrets.json", Encoding.UTF8);
                    Console.WriteLine("Secrets applied to configuration parameters.");
                    return JsonDocument.Parse(json);
                }
                catch
                {
                    Console.WriteLine($"Loading appsettings.{WorkBench.EnvironmentName}.json without merging with secrets");
                    Console.WriteLine("(because file '/k8s/secrets/server-secrets.json' was not found).");
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        private static T ReplaceSecrets<T>(T config) where T : LightConfig<T>
        {
            if (Secrets is null)
            {
                return config;
            }

            string json = config.ToJsonString();

            foreach (JsonProperty secret in Secrets.RootElement.EnumerateObject())
                json = json.Replace($"\\u003C{secret.Name}\\u003E", $"{secret.Value.AsString()}");

            return json.ToObject<T>();
        }
        private static JsonDocument LoadGeneralConfig()
        {
            var assembly = Assembly.Load("Liquid.Platform");
            var resourceName = $"Liquid.Platform.appsettings.{WorkBench.EnvironmentName}.json";

#pragma warning disable IDE0063 // Use simple 'using' statement
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream is null)
                    throw new LightException($"The embeded resource '{resourceName}' could not be found. Check settings file properties");

                using (StreamReader reader = new(stream))
                {
                    string json = reader.ReadToEnd();
                    return JsonDocument.Parse(json);
                }
            }
#pragma warning restore IDE0063 // Use simple 'using' statement
        }

        /// <summary>
        /// Is Responsible for get the section from the Workbench 
        /// and validate the configuration
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="section"></param>
        /// <returns>The section configuration object</returns>
        public static T LoadConfig<T>(string section) where T : LightConfig<T>
        {
            if (string.IsNullOrEmpty(section))
                throw new Exception("Not possible to find a valid configuration section with name 'Null'");

            // Load given section from Workbench
            var config = WorkBench.Configuration.GetSection(section).Get<T>();

            if (config is null)
            {
                // Tries to get from general configuration files
                config = GetGeneralSection<T>(section);

                if (config is null)
                {
                    throw new LightException($"Not found a valid configuration section with name '{section}'.");
                }
            }

            // Merge section configuration with secrets 
            config = ReplaceSecrets(config);

            // Verify if there's any errors from the configuration
            var validationErrors = ValidateConfig(config);

            if (validationErrors.Count > 0)
            {   // if there's any exception trhows the errors
                throw new InvalidConfigurationException(validationErrors);
            }
            // returns the section configuration 
            return config;
        }

        private static T GetGeneralSection<T>(string section) where T : LightConfig<T>
        {
            var sections = section.Split(":");

            //Tries to get a root config session
            if (!_generalConfig.RootElement.TryGetProperty(sections[0], out JsonElement config))
                return null;
            else
            {
                //If it is a child config session, tries to get it
                if (sections.Length == 2)
                {
                    if (config.TryGetProperty(sections[1], out JsonElement child))
                        config = child;
                    else
                        return null;
                }

                return config.ToObject<T>();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="config"></param>
        /// <returns>Exception List</returns>
        private static Dictionary<string, object[]> ValidateConfig<T>(T config) where T : LightConfig<T>
        {
            Dictionary<string, object[]> _inputValidationErrors = new();

            config.Validate();
            var result = config.Validator.Validate(config);
            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    // The method receive the error code to add on errors list of input validation.
                    _inputValidationErrors.TryAdd(error.Key, new object[] { error.Value });
                }
            }
            // By reflection, browse viewModel by identifying all attributes and lists for validation.  
            foreach (FieldInfo fieldInfo in config.GetType().GetFields())
            {
                if (fieldInfo.GetValue(config) is IList children)
                {
                    // Validate each of its members 
                    foreach (var item in children)
                    {
                        if ((item.GetType().BaseType != typeof(object))
                                && ((item.GetType().BaseType.IsGenericType &&
                                item.GetType().BaseType.GetGenericTypeDefinition() == typeof(LightConfig<>))))
                        {
                            dynamic obj = item;
                            // The method receive the error code to add on errors list of input validation.
                            foreach (KeyValuePair<string, string> error in ValidateConfig(obj))
                                _inputValidationErrors.TryAdd(error.Key, new object[] { error.Value });
                        }
                    }
                }
            }

            return _inputValidationErrors;
        }
    }
#pragma warning restore IDE0044 // Add readonly modifier
}
