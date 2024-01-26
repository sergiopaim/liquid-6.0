using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Liquid.Runtime.OpenApi
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class GenerateSwaggerCommand : WorkBenchCommand, IWorkBenchCommand
    {
        private readonly static SwaggerGenerationConfig config = LightConfigurator.LoadConfig<SwaggerGenerationConfig>("SwaggerGeneration");

        private readonly bool setup;
        private readonly string path;
        private readonly string fileName;

        public override bool Execute()
        {
            if (setup)
            {
                ISwaggerProvider provider = (ISwaggerProvider)Host?.Services?.GetService(typeof(ISwaggerProvider));

                var swagger = provider.GetSwagger("v1", null, "/");
                swagger = OpenApiMiddleware.FilterSchemas(swagger);

                using var textWriter = new StringWriter();
                var jsonWriter = new OpenApiJsonWriter(textWriter);

                swagger.SerializeAsV3(jsonWriter);

                string json = SwaggerConventions.ApllyConventions(textWriter.ToString());

                if (IsReactiveHub())
                {
                    json = AddReactiveHubEndpoints(json);
                }

                Publish(json);
            }
            else
            {
                Console.Error.WriteLine($"GenerateSwagger command: Invalid parms ({Args()})");
                Console.Error.WriteLine();
                Console.Error.WriteLine($"Expected: GenerateSwagger [fullDomainName] [releaseName]");
                Console.Error.WriteLine();
                Console.Error.WriteLine($"Example: GenerateSwagger domain/core/clients 1.25");
            }

            return false;
        }

        private static string AddReactiveHubEndpoints(string json)
        {
            //Currently only SignalR hub is supported. In the future, the endpoints should be generated for each corresponding hub cartridge
            const string signalREndpoints = "    \"/events/negotiate\": {\r\n            \"post\": {\r\n                \"tags\": [\r\n                    \"SignalRHub\"\r\n                ],\r\n                \"summary\": \"SignalR Hub negotiate\",\r\n                \"description\": \"Starts the SignalR hub connection negotiation\",\r\n                \"operationId\": \"signalr-hub-negotiate\",\r\n                \"parameters\": [\r\n                    {\r\n                        \"name\": \"token\",\r\n                        \"in\": \"query\",\r\n                        \"description\": \"JWT authorization\",\r\n                        \"schema\": {\r\n                            \"type\": \"string\"\r\n                        }\r\n                    },\r\n                    {\r\n                        \"name\": \"negotiateVersion\",\r\n                        \"in\": \"query\",\r\n                        \"description\": \"Negotiation version\",\r\n                        \"schema\": {\r\n                            \"type\": \"integer\"\r\n                        }\r\n                    }\r\n                ],\r\n                \"responses\": {\r\n                    \"200\": {\r\n                        \"description\": \"\"\r\n                    },\r\n                    \"401\": {\r\n                        \"description\": \"\"\r\n                    }\r\n                }\r\n            },\r\n            \"options\": {\r\n                \"tags\": [\r\n                    \"SignalRHub\"\r\n                ],\r\n                \"summary\": \"SignalR Hub negotiate preflight\",\r\n                \"description\": \"Preflight for starting SignalR hub connection negotiation\",\r\n                \"operationId\": \"signalr-hub-negotiate-preflight\",\r\n                \"parameters\": [\r\n                    {\r\n                        \"name\": \"token\",\r\n                        \"in\": \"query\",\r\n                        \"description\": \"JWT authorization\",\r\n                        \"schema\": {\r\n                            \"type\": \"string\"\r\n                        }\r\n                    },\r\n                    {\r\n                        \"name\": \"negotiateVersion\",\r\n                        \"in\": \"query\",\r\n                        \"description\": \"Negotiation version\",\r\n                        \"schema\": {\r\n                            \"type\": \"integer\"\r\n                        }\r\n                    }\r\n                ],\r\n                \"responses\": {\r\n                    \"204\": {\r\n                        \"description\": \"\"\r\n                    },\r\n                    \"401\": {\r\n                        \"description\": \"\"\r\n                    }\r\n                }\r\n            }\r\n        }";
            const string signalRTags = "\"tags\": [\r\n    {\r\n      \"name\": \"SignalR\",\r\n      \"description\": \"Endpoints of SignalR real-time hub\"\r\n    }\r\n  ],\r\n  ";
            if (json.Contains("\"paths\": {}"))
                json = json.Replace("\"paths\": {}", $"\"paths\": {{\r\n{signalREndpoints}}}");
            else
                json = json.Replace("\"paths\": {", $"\"paths\": {{\r\n{signalREndpoints},");

            json = json.Replace("\"security\": ", $"{signalRTags}\"security\": ");

            return json;
        }

        private void Publish(string json)
        {
            var client = CloudStorageAccount.Parse(config.AzureStorageConnStr).CreateCloudBlobClient();
            var container = client.GetContainerReference("swagger");
            container.CreateIfNotExists();
            var blob = container.GetBlockBlobReference($"{path}/{fileName}");

            blob.UploadText(json, Encoding.UTF8);

            Console.WriteLine($"swagger.json published in the storage for 'devops artifacts' as '{path}/{fileName}'");
            Console.WriteLine("");
        }

        public GenerateSwaggerCommand(IWebHost host, string[] args, bool isReactiveHub = false) : base(host, args, isReactiveHub)
        {
            if (args?.Length == 2 && args[0].Contains('/'))
            {
                path = args[0];
                fileName = $"{args[1]}.json";

                setup = true;
            }
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
