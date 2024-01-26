using Liquid.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Liquid.Repository
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public abstract class LightDataEventModel<T> : LightModel<T> where T : LightModel<T>, ILightModel, new()
    {
        protected LightDataEventModel() : base() { }
        public string Command { get; set; }
        public DateTime IngestedAt { get; set; }
        [JsonIgnore]
        public override List<string> Attachments { get; set; }

        public override void Validate() { }

    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
