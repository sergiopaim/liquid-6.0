using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;

namespace Liquid.Platform
{
    /// <summary>
    /// A service account user credentials
    /// </summary>
    public class ServiceAccountVM : LightViewModel<ServiceAccountVM>
    {
        /// <summary>
        /// Service account id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Ephemeral (non encripted and non stored) service account authentication secret
        /// </summary>
        public string Secret { get; set; }
        /// <summary>
        /// Service account name 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Service account (admin) email
        /// </summary>
        public string Email { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
        {
            RuleFor(i => false).Equal(true).WithError("This ViewModel type can only be used as response");
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
