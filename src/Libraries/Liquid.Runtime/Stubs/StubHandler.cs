using Liquid.Base;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Liquid.Runtime
{
    /// <summary>
    /// Helper class for loading static testing data
    /// </summary>
    public static class StubHandler
    {
        /// <summary>
        /// Value that represents a dynamic value that enters the stub and should be returned by it 
        /// (This is a workaround until the full feature of returning input values of the stub is implemented)
        /// </summary>
        public static readonly string TRANSIENT_VALUE = "TRANSIENT_STUB_VALUE";

        /// <summary>
        /// Get entity names with files to Seedseed files on seed folder 
        /// </summary> 
        /// <returns>Seed file names</returns>
        public static string[] GetEntitysWithFilesToSeed()
        {
            if (Directory.Exists($"{Directory.GetCurrentDirectory()}/Seed"))
            {
                string[] files = Directory.GetFiles($"{Directory.GetCurrentDirectory()}/Seed", "*.json")
                         .Select(file => Path.GetFileName(file)).ToArray();

                for (int j = 0; j < files.Length; j++)
                    files[j] = files[j].Split(".")[0];

                return files.Distinct().ToArray();
            }
            return Array.Empty<string>();
        }

        /// <summary>
        /// Get attachment files to seed on seed folder by environment and entity name
        /// </summary>
        /// <param name="dataSetType">Type of dataSet being seeded</param> 
        /// <param name="fileEntityName">LightModel entity name</param>
        /// <returns>Attachment file names</returns>
        public static Dictionary<string, byte[]> GetAttachmentFilesNames(string dataSetType, string fileEntityName)
        {
            var files = new Dictionary<string, byte[]>();

            string seedDir = $"{Directory.GetCurrentDirectory()}/Seed/Attachments.{fileEntityName}.{dataSetType}";
            if (Directory.Exists(seedDir))
                foreach (var name in Directory.GetFiles(seedDir).Select(file => Path.GetFileName(file)))
                    files.Add(name, File.ReadAllBytes(seedDir + "/" + name));

            return files;
        }

        private static string StripComments(string text)
        {
            List<string> strippedText = new();
            foreach (string line in text.Split('\n'))
            {
                if (!line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                    strippedText.Add(line);
            }
            return string.Join("\n", strippedText);
        }

        private static string SQLDateTimeToISOFormat(string text)
        {
            return Regex.Replace(text, "\\\"([0-9]+\\-[0-9]+\\-[0-9]+)(\\s)([0-9]+\\:[0-9]+\\:[0-9]+.*)(\\s(UTC))\\\"", "\"$1T$3Z\"");
        }

        /// <summary>
        /// Get Stub files on subdirectory folder by environment
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataFileName">name of data file</param>
        /// <param name="suffix"></param>
        /// <param name="subDirectory"></param>
        /// <returns></returns>
        public static T GetStubData<T>(string dataFileName, string suffix = "", string subDirectory = "")
        {
            var pathPrefix = Directory.GetCurrentDirectory();

            if (!string.IsNullOrEmpty(subDirectory))
                pathPrefix += $"/{subDirectory}";

            var filePath = $"{pathPrefix}/{dataFileName}.";

            filePath += string.IsNullOrEmpty(suffix) ?
                "json" : $"{suffix}.json";

            string json;
            if (File.Exists(filePath))
                json = File.ReadAllText(filePath, Encoding.UTF8);
            else
            {
                WorkBench.ConsoleWriteLine();
                WorkBench.ConsoleWriteErrorLine($"Failed to find test data file '{Encoding.UTF8.GetString(Encoding.ASCII.GetBytes(filePath))}'");
                WorkBench.ConsoleWriteHighlightedLine("Check if file does exist and if it's 'Copy if newer' build property is set.");
                WorkBench.ConsoleWriteLine();
                return (T) (object) null;
            }

            json = StripComments(json);
            json = SQLDateTimeToISOFormat(json);

            try
            {
                return ConvertDynamicTags(json).ToObject<T>();
            }
            catch (Exception ex)
            {
                throw new LightException($"Failed to deserialize invalid JSON data in file {filePath}. Check if its JSON content is well formed.", ex);
            }
        }

        /// <summary>
        /// Convert the tags on dynamic data {{TODAY-n days}} and {{NOW -n minutes}} to current value
        /// </summary>
        /// <param name="jsonString">string contained the tags</param>
        /// <returns>Return the string with replace Tags</returns>
        private static string ConvertDynamicTags(string jsonString)
        {
            DateTime today = WorkBench.Today;
            DateTime now = WorkBench.UtcNow;

            jsonString = jsonString.Replace("{{MIN_DATETIME}}", "0001-01-01T00:00:00");
            jsonString = jsonString.Replace("{{TODAY}}\"", today.ToString("yyyy-MM-dd") + "T00:00:00.000Z\"");
            jsonString = jsonString.Replace("{{TODAY}}", today.ToString("yyyy-MM-dd"));
            jsonString = jsonString.Replace("{{NOW}}", now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            jsonString = jsonString.Replace("{{TRANSIENT}}", TRANSIENT_VALUE);

            MatchCollection todays = Regex.Matches(jsonString, @"\{{TODAY[\-|\+][0-9]+}}""", RegexOptions.None);
            foreach (Match item in todays.Cast<Match>())
            {
                DateTime newDate = today.AddDays(Double.Parse(Regex.Match(item.Value, @"[\-|\+][0-9]+").Value));
                jsonString = jsonString.Replace(item.Value, newDate.ToString("yyyy-MM-dd") + "T00:00:00.000Z\"");
            }

            MatchCollection todaysPlusTime = Regex.Matches(jsonString, @"\{{TODAY[\-|\+][0-9]+}}", RegexOptions.None);
            foreach (Match item in todaysPlusTime.Cast<Match>())
            {
                DateTime newDate = today.AddDays(Double.Parse(Regex.Match(item.Value, @"[\-|\+][0-9]+").Value));
                jsonString = jsonString.Replace(item.Value, newDate.ToString("yyyy-MM-dd"));
            }

            MatchCollection nows = Regex.Matches(jsonString, @"\{{NOW[\-|\+][0-9]+}}", RegexOptions.None);
            foreach (Match item in nows.Cast<Match>())
            {
                DateTime newDate = now.AddMinutes(Double.Parse(Regex.Match(item.Value, @"[\-|\+][0-9]+").Value));
                jsonString = jsonString.Replace(item.Value, newDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            }

            while (jsonString.Contains("{{GUID}}"))
                jsonString = jsonString.ReplaceFirst("{{GUID}}", Guid.NewGuid().ToString());

            while (jsonString.Contains("{{SHORTNER}}"))
                jsonString = jsonString.ReplaceFirst("{{SHORTNER}}", Base62.Encode());

            return jsonString;
        }

        /// <summary>
        /// Handle http invoke
        /// </summary>
        /// <param name="context"></param>
        /// <param name="pathToCheck"></param>
        /// <returns></returns>
        public static bool HandleHttpInvoke(ref HttpContext context, string pathToCheck)
        {
            if (pathToCheck.StartsWith("/forceStub"))
            {
                if (!WorkBench.IsDevelopmentEnvironment && !WorkBench.IsIntegrationEnvironment)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return false;
                }

                string message;
                string mode;
                pathToCheck = pathToCheck.Trim('/');
                if (pathToCheck == "forceStub")
                {
                    mode = WorkBench.ForceStubMicroserviceCalls ? "enabled" : "disabled";
                    message = $"Force stub mode is currently set to {mode}. Use `/forceStub/<enable|disable>` to change it.";
                    context.Response.WriteAsync(message);
                    return false;
                }

                var invalidPath = false;
                bool? forceStub = null;

                // format: `forceStub/<enable|disable>`
                var splitPath = pathToCheck.Split('/');
                if (splitPath.Length != 2)
                    invalidPath = true;

                else switch (splitPath[1])
                    {
                        case "enable":
                            forceStub = true;
                            break;
                        case "disable":
                            forceStub = false;
                            break;
                        default:
                            invalidPath = true;
                            break;
                    }

                if (invalidPath)
                {
                    message = "Failed to set stub mode. Correct usage: `/forceStub/<enable|disable>`";
                    context.Response.WriteAsync(new { message }.ToJsonString());
                    return false;
                }

                WorkBench.ForceStubMicroserviceCalls = forceStub.Value;
                mode = forceStub.Value ? "enabled" : "disabled";
                message = $"Setting force stub mode to {mode}.";

                context.Response.WriteAsync(message);
                return false;
            }
            else
                return true;
        }
    }
}
