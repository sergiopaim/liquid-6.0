using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;

namespace Microservice.ViewModels
{
    /// <summary>
    /// A user's profile with its editable attributes
    /// </summary>
    public class WebPushEndpointVM : LightViewModel<WebPushEndpointVM>
    {
        /// <summary>
        /// The id of the device
        /// </summary>
        public string DeviceId { get; set; }
        /// <summary>
        /// The web endpoint for doing a push
        /// </summary>
        public string PushEndpoint { get; set; }
        /// <summary>
        /// The P256DH key
        /// </summary>
        public string PushP256DH { get; set; }
        /// <summary>
        /// The WebPush authorization token
        /// </summary>
        public string PushAuth { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
        {
            RuleFor(i => i.DeviceId).NotEmpty().WithError("deviceId must not be empty");
            RuleFor(i => i.PushEndpoint).NotEmpty().WithError("pushEndpoint must not be empty");
            RuleFor(i => i.PushP256DH).NotEmpty().WithError("pushP256DH must not be empty");
            RuleFor(i => i.PushAuth).NotEmpty().WithError("pushAuth must not be empty");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
