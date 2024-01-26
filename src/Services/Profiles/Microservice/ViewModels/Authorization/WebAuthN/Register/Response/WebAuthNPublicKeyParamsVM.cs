using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;

namespace Microservice.ViewModels
{
    /// <summary>
    /// Public-key parameters used in WebAuthN authentication process
    /// </summary>
    public class WebAuthNPublicKeyParamsVM : LightViewModel<WebAuthNPublicKeyParamsVM>
    {
        /// <summary>
        /// PK type
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// PK algorithm
        /// </summary>
        public int Alg { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
        {
            RuleFor(i => false).Equal(true).WithError("This ViewModel type can only be used as response");
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
