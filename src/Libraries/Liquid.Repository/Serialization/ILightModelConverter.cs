using Liquid.Interfaces;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Liquid.Repository
{
    /// <summary>
    /// Json converter for ILightModel sub types
    /// </summary>
    public class ILightModelConverter : JsonConverter<ILightModel>
    {
        /// <summary>
        /// Deserializes a LightModel type as a ILightModel
        /// </summary>
        public override ILightModel Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize(reader.GetString(), typeToConvert, options) as ILightModel;
        }

        /// <summary>
        /// Serializes a ILightModel object to json
        /// </summary>
        public override void Write(
            Utf8JsonWriter writer,
            ILightModel model,
            JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, model, options);
        }
    }    
}
