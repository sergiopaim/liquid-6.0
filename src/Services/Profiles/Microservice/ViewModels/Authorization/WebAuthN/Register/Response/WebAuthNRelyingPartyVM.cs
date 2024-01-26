using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;

namespace Microservice.ViewModels
{ 
    /// <summary>
    /// Relying party of WebAuthN
    /// </summary>
    public class WebAuthNRelyingPartyVM : LightViewModel<WebAuthNRelyingPartyVM>
    {
        /// <summary>
        /// Relying party id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Relying party name
        /// </summary>
        public string Name { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            RuleFor(i => false).Equal(true).WithError("This ViewModel type can only be used as response");
        }
    }
}

