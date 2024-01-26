using Microsoft.Azure.Cosmos.Spatial;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Liquid.Base
{
    /// <summary>
    /// Json converter for Cosmos.Spatial.Point 
    /// </summary>
    public class CosmosSpatialPointConverter : JsonConverter<Point>
    {
        /// <summary>
        /// Deserializes json as a Cosmos.Spatial.Point type 
        /// </summary>
        public override Point Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            Point point = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                // Get the key.
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString();

                    if (propertyName == "coordinates")
                    {
                        reader.Read();
                        var coordinates = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

                        point = new(coordinates[1].AsDouble() ?? 0, coordinates[0].AsDouble() ?? 0);

                    }
                }
            }

            if (point is null)
            {
                var e = new JsonException();
                e.FilterRelevantStackTrace();
                throw e;
            }
            else
                return point;
        }

        /// <summary>
        /// Serializes a Cosmos.Spatial.Point object to json
        /// </summary>
        public override void Write(
            Utf8JsonWriter writer,
            Point point,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("type", "Point");
            writer.WritePropertyName("coordinates");
            writer.WriteStartArray();
            writer.WriteNumberValue(point.Position.Latitude);
            writer.WriteNumberValue(point.Position.Longitude);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
