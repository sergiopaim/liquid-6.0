using FluentValidation;
using Liquid.Repository;
using Liquid.Runtime;

namespace Microservice.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class WebPushEndpoint : LightValueObject<WebPushEndpoint>
    {   
        /// <summary>
        /// 
        /// </summary>
        public string DeviceId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string PushEndpoint { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string PushP256DH { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string PushAuth { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public override void Validate()
        {
            RuleFor(i => i.DeviceId).NotEmpty().WithError("deviceId must not be empty");
            RuleFor(i => i.PushEndpoint).NotEmpty().WithError("pushEndpoint must not be empty");
            RuleFor(i => i.PushP256DH).NotEmpty().WithError("pushP256DH must not be empty");
            RuleFor(i => i.PushAuth).NotEmpty().WithError("pushAuth must not be empty");
        }
    }
}