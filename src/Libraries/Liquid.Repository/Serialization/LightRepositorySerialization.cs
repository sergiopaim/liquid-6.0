using Liquid.Base;
using Microsoft.Azure.Cosmos;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Liquid.Repository
{
    /// <summary>
    /// General util class for Json serialization for repository types
    /// </summary>
    public static class LightRepositorySerialization
    {
        /// <summary>
        /// Default Json options for serializations (case insensitive)
        /// </summary>
        public static readonly JsonSerializerOptions JsonDefault = new()
                                                                   {
                                                                       NumberHandling = JsonNumberHandling.AllowReadingFromString,
                                                                       PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                       Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                                                                       Converters = {
                                                                                        new ILightModelConverter(),
                                                                                        new CosmosSpatialPointConverter(),
                                                                                        new CosmosSpatialPositionConverter(),
                                                                                    }
                                                                   };

        /// <summary>
        /// Default options (case insensitive)
        /// </summary>
        public static readonly JsonSerializerOptions JsonIgnoreCase = new()
                                                                      {
                                                                          NumberHandling = JsonNumberHandling.AllowReadingFromString,
                                                                          PropertyNameCaseInsensitive = true,
                                                                          Converters = {
                                                                                           new ILightModelConverter(),
                                                                                           new CosmosSpatialPointConverter(),
                                                                                           new CosmosSpatialPositionConverter(),
                                                                                       }
                                                                      };

        /// <summary>
        /// Default Linq options for serializations (case insensitive)
        /// </summary>
        public static readonly CosmosLinqSerializerOptions LinqDefault = new()
                                                                         {
                                                                             PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                                                                         };
    }
}