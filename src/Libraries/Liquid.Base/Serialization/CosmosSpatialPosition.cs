using Microsoft.Azure.Cosmos.Spatial;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Liquid.Base
{
    /// <summary>
    /// Json converter for Cosmos.Spatial.Position 
    /// </summary>
    public class CosmosSpatialPositionConverter : JsonConverter<Position>
    {
        /// <summary>
        /// Deserializes json as a Cosmos.Spatial.Position type 
        /// </summary>
        public override Position Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var coordinates = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

            Position position = new(coordinates[1].AsDouble() ?? 0, coordinates[0].AsDouble() ?? 0);

            if (position is null)
            {
                var e = new JsonException();
                e.FilterRelevantStackTrace();
                throw e;
            }
            else
                return position;
        }

        /// <summary>
        /// Serializes a Cosmos.Spatial.Position object to json
        /// </summary>
        public override void Write(
            Utf8JsonWriter writer,
            Position position,
            JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(position.Latitude);
            writer.WriteNumberValue(position.Longitude);
            writer.WriteEndArray();
        }
    }    
}
