using Liquid.Platform;
using Liquid.Repository;
using Liquid.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Microservice.Models
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA1304 // Specify CultureInfo
    public class WebPushChannel : LightValueObject<WebPushChannel>
    {
        public List<WebPushEndpoint> Endpoints { get; set; } = new();
        public List<string> NotificationTypes { get; set; } = new();

        [JsonIgnore]
        public bool HasAvailableEndpoints => Endpoints.Count > 0;
        public bool IsValidNotificationType(string type)
        {
            return type == NotificationType.Account.Code || NotificationTypes.Any(n => n.ToLower() == type.ToLower());
        }

        public override void Validate()
        {

        }
    }
#pragma warning restore CA1304 // Specify CultureInfo
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}