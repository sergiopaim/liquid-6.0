using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;
using System.Collections.Generic;

namespace Microservice.ViewModels
{
    /// <summary>
    /// The view model with the information so the device can register with the WebAuthN authenticator
    /// </summary>
    public class WebAuthNCredentialCreationVM : LightViewModel<WebAuthNCredentialCreationVM>
    {
        /// <summary>
        /// The challenge that must be used to register with the WebAuthN authenticator, base 64 encoded
        /// </summary>
        public string Challenge { get; set; }
        /// <summary>
        /// Relying Party
        /// </summary>
        public WebAuthNRelyingPartyVM RP { get; set; }
        /// <summary>
        /// The user associated with the request
        /// </summary>
        public WebAuthNUserVM User { get; set; }
        /// <summary>
        /// Public-key credentials
        /// </summary>
        public List<WebAuthNPublicKeyParamsVM> PubKeyCredParams { get; set; }
        /// <summary>
        /// Credentials to Exclude
        /// </summary>
        public List<WebAuthNCredentialsVM> ExcludeCredentials { get; set; }
        /// <summary>
        /// Attestation token
        /// </summary>
        public string Attestation { get; set; }
        /// <summary>
        /// User verification token
        /// </summary>
        public string UserVerification { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
        {
            RuleFor(i => false).Equal(true).WithError("This ViewModel type can only be used as response");
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
