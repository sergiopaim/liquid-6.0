using Liquid.Base;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Liquid.Runtime
{
    /// <summary>
    /// Apply Open Api Conventions
    /// An API specification needs to specify the responses for all API operations.
    /// </summary>
    public static class SwaggerConventions
    {
        /// <summary>
        /// Apply all conventions for AMAW microservice on http status code.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        internal static string ApllyConventions(string json)
        {
            JsonNode o = JsonNode.Parse(json);

            foreach (var apiPath in o["paths"].AsObject())
            {
                foreach (var httpVerbs in apiPath.Value.AsObject())
                {
                    switch (httpVerbs.Key)
                    {
                        case "get":
                            CreateGetResponses(httpVerbs.Value["responses"].AsObject());
                            break;
                        case "post":
                            CreatePostResponses(httpVerbs.Value["responses"].AsObject());
                            break;
                        case "put":
                            CreatePutResponses(httpVerbs.Value["responses"].AsObject());
                            break;
                        case "delete":
                            CreateDeleteResponses(httpVerbs.Value["responses"].AsObject());
                            break;
                    }
                }
            }

            return JsonSerializer.Serialize(o, LightGeneralSerialization.WriteIndented);
        }

        /// <summary>
        /// Gets convention object with the given type.
        /// </summary>
        /// <param name="conv">name of the convention</param>
        /// <returns>Returns convention objects</returns>
        private static JsonNode GetConventionProperty(string conv)
        {
            return conv switch
            {
                "200" => BuildConventionProperty("Ok"),
                "204" => BuildConventionProperty("No Content"),
                "400" => BuildConventionProperty("Bad Request"),
                "401" => BuildConventionProperty("Unauthorized"),
                "403" => BuildConventionProperty("Forbidden"),
                "409" => BuildConventionProperty("Conflict"),
                _ => BuildConventionProperty("Ok"),
            };
        }

        private static JsonNode BuildConventionProperty(string description)
        {
            return JsonSerializer.Deserialize<JsonNode>($"{{\"description\":\"{description}\"}}");
        }

        private static void InsertConventions(JsonObject responses, List<string> conventions)
        {
            foreach (var conv in conventions)
            {
                if (!responses.Any(x => x.Key == conv))
                    responses.Add(conv, GetConventionProperty(conv));
            }
        }

        private static void CreateDeleteResponses(JsonObject responses)
        {
            InsertConventions(responses, new List<string> { "200", "204", "400", "401", "403" });
        }

        private static void CreatePutResponses(JsonObject responses)
        {
            InsertConventions(responses, new List<string> { "200", "204", "400", "401", "403", "409" });
        }

        private static void CreatePostResponses(JsonObject responses)
        {
            InsertConventions(responses, new List<string> { "200", "400", "401", "403", "409" });
        }

        private static void CreateGetResponses(JsonObject responses)
        {
            InsertConventions(responses, new List<string> { "200", "204", "400", "401", "403" });
        }
    }
}