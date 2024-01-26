using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;
using System;

namespace Microservice.ViewModels
{
    /// <summary>
    /// The view model with the new JWT created
    /// </summary>
    public class TokenVM : LightViewModel<TokenVM>
    {
        /// <summary>
        /// The account id to which the JWT was issued
        /// </summary>
        public string IssuedTo { get; set; }
        /// <summary>
        /// The JWT
        /// </summary>
        public string Token { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
        {
            RuleFor(i => false).Equal(true).WithError("This ViewModel type can only be used as response");
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
