using Liquid.Base;
using Liquid.Repository;
using Liquid.Runtime;
using Microsoft.Azure.Cosmos;
using System.IO;
using System.Text.Json;

internal sealed class CosmosSystemTextJsonSerializer : CosmosSerializer
{
    public override T FromStream<T>(Stream stream)
    {
        if (stream.CanSeek && stream.Length == 0)
            return default;

        if (typeof(Stream).IsAssignableFrom(typeof(T)))
            return (T)(object)stream;

        using (stream)
        {
            return (T)JsonSerializer.Deserialize(stream, typeof(T), LightRepositorySerialization.JsonDefault);
        }
    }

    public override Stream ToStream<T>(T input)
    {
        var streamPayload = new MemoryStream();
        JsonSerializer.Serialize(streamPayload, input, typeof(T), LightRepositorySerialization.JsonDefault);
        streamPayload.Position = 0;
        return streamPayload;
    }
}
