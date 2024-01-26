using Liquid.Interfaces;
using System.Text.Json.Serialization;

namespace Liquid.Repository
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public abstract class LightOptimisticModel<T> : LightModel<T> where T : LightModel<T>, ILightModel, new()
    {
        protected LightOptimisticModel() : base() { }
        public override abstract void Validate();
        [JsonPropertyName("_etag")]
        public string ETag { get; set; }

    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
