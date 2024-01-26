using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;

namespace Microservice.Models
{
    /// <summary>
    /// A user's profile with pending (not confirmed and/or approved) changes in its attributes
    /// </summary>

    public class ProfileWithPendingChangesVM : LightViewModel<ProfileWithPendingChangesVM>
    {
        /// <summary>
        /// User's id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The user's email
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// Indicates whether the email has been validated
        /// </summary>
        public bool EmailIsValid { get; set; }
        /// <summary>
        /// The user's phone number
        /// </summary>
        public string Phone { get; set; }
        /// <summary>
        /// Indicates whether the phone number has been validated
        /// </summary>
        public bool PhoneIsValid { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
        {
            RuleFor(i => false).Equal(true).WithError("This ViewModel type can only be used as response");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
