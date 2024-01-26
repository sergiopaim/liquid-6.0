using System.Text.Json;
using System.Text.Json.Serialization;

namespace Liquid.Base
{
    /// <summary>
    /// General util class for Json serialization for repository types
    /// </summary>
    public static class LightGeneralSerialization
    {
        /// <summary>
        /// Default options (case insensitive)
        /// </summary>
        public static readonly JsonSerializerOptions Default = new()
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = {
                                                                                    new CosmosSpatialPointConverter(),
                                                                                    new CosmosSpatialPositionConverter(),
                                                                                }
        };

        /// <summary>
        /// Default options (case insensitive)
        /// </summary>
        public static readonly JsonSerializerOptions WriteIndented = new()
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = {
                                                                                          new CosmosSpatialPointConverter(),
                                                                                          new CosmosSpatialPositionConverter(),
                                                                                      }
        };

        /// <summary>
        /// Default options (case insensitive)
        /// </summary>
        public static readonly JsonSerializerOptions IgnoreCase = new()
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            PropertyNameCaseInsensitive = true,
            Converters = {
                                                                                       new CosmosSpatialPointConverter(),
                                                                                       new CosmosSpatialPositionConverter(),
                                                                                   }
        };
    }
}