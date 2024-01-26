using Liquid.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Liquid.Repository
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class LightAttachment : ILightAttachment
    {
        [JsonPropertyName("_id")]
        public virtual string Id { get; set; }
        [JsonPropertyName("_name")]
        public virtual string Name { get; set; }
        [JsonPropertyName("_rid")]
        public virtual string ResourceId { get; set; }
        [JsonPropertyName("contentType")]
        public virtual string ContentType { get; set; }
        [JsonIgnore]
        public virtual Stream MediaStream { get; set; }
        [JsonIgnore]
        public string MediaLink { get; set; }
        public virtual string ETag { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string> Attachments { get; set; }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
