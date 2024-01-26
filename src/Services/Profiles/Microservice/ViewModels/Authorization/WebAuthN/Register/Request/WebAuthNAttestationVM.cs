using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;

namespace Microservice.ViewModels
{
    /// <summary>
    /// Client data payload
    /// </summary>
    public class ClientDataJSON
    {
        /// <summary>
        /// Challenge used during authentication
        /// </summary>
        public string Challenge { get; set; }
        /// <summary>
        /// Request origin
        /// </summary>
        public string Origin { get; set; }
        /// <summary>
        /// Authentication type
        /// </summary>
        public string Type { get; set; }
    }

    /// <summary>
    /// Attestation of WebauthN authentication process
    /// </summary>
    public class WebAuthNAttestationVM : LightViewModel<WebAuthNAttestationVM>
    {
        /// <summary>
        /// Attestation content
        /// </summary>
        public string AttestationObject { get; set; }
        /// <summary>
        /// Client data
        /// </summary>
        public string ClientDataJSON { get; set; }
        /// <summary>
        /// Attestation signature
        /// </summary>
        public string Signature { get; set; }
        /// <summary>
        /// Attestation authenticator
        /// </summary>
        public string AuthenticatorData { get; set; }


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
        {
            RuleFor(i => i.ClientDataJSON).NotEmpty().WithError("clientDataJSON must not be empty");
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}

