using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;

namespace Microservice.ViewModels
{ 
    /// <summary>
    /// Request of a WebAuthN credential
    /// </summary>
    public class WebAuthNRequestVM : LightViewModel<WebAuthNRequestVM>
    {
        /// <summary>
        /// Credential's id
        /// </summary>
        public string CredentialId { get; set; }
        /// <summary>
        /// Type of authentication
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Attestation response
        /// </summary>
        public WebAuthNAttestationVM Response { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            RuleFor(i => i.CredentialId).NotEmpty().WithError("credentialId must not be empty");
            RuleFor(i => i.Type).NotEmpty().WithError("type must not be empty");
            RuleFor(i => i.Response).NotEmpty().WithError("response must not be empty");
        }
    }
}

