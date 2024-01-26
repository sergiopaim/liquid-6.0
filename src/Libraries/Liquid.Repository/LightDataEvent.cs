using Liquid.Activation;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Liquid.Repository
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class LightDataEvent<T> : LightMessage<LightDataEvent<T>, LightDataEventCMD>
    {
        public string EntityName { get; set; }
        public DateTime IngestedAt { get; set; }
        public T Payload { get; set; }
        [JsonIgnore]
        public override string TokenJwt
        {
            get
            {
                return null;
            }
            set { }
        }

        public override void Validate() { }

        public override Dictionary<string, object> GetUserProperties()
        {
            return new()
            {
                {
                    nameof(EntityName),
                    EntityName
                },
                {
                    nameof(CommandType),
                    CommandType
                }
            };
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}