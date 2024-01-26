using System.Text.Json.Serialization;

namespace Microservice.Models
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class WebAuthNPublicKeyES256
    {
        [JsonPropertyName("1")]
        public string KeyType { get; set; }
        [JsonPropertyName("3")]
        public string Algorithm { get; set; }
        [JsonPropertyName("-1")]
        public string Curve { get; set; }
        [JsonPropertyName("-2")]
        public string X { get; set; }
        [JsonPropertyName("-3")]
        public string Y { get; set; }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}

