using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;
using System.Collections.Generic;

namespace Microservice.ViewModels
{
    /// <summary>
    /// Payload of a WebAuthN credential request
    /// </summary>
    public class WebAuthNCredentialRequestVM : LightViewModel<WebAuthNCredentialRequestVM>
    {
        /// <summary>
        /// Authentication challenge
        /// </summary>
        public string Challenge { get; set; }
        /// <summary>
        /// Requester domain
        /// </summary>
        public string RpId { get; set; }
        /// <summary>
        /// List off allowed credentials 
        /// </summary>
        public List<WebAuthNCredentialsVM> AllowCredentials { get; set; }
        /// <summary>
        /// User verification token
        /// </summary>
        public string UserVerification { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            RuleFor(i => false).Equal(true).WithError("This ViewModel type can only be used as response");
        }
    }
}
