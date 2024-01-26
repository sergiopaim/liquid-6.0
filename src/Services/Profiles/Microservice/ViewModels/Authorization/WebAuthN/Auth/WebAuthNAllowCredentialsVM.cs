using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;
using System.Collections.Generic;

namespace Microservice.ViewModels
{
    /// <summary>
    /// WebAuthN credential
    /// </summary>
    public class WebAuthNCredentialsVM : LightViewModel<WebAuthNCredentialsVM>
    {
        /// <summary>
        /// Credential's id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Type of credential
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// List of transports
        /// </summary>
        public List<string> Transports { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            RuleFor(i => false).Equal(true).WithError("This ViewModel type can only be used as response");
        }
    }
}
