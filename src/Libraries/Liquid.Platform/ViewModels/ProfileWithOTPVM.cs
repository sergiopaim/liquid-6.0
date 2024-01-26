using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;

namespace Liquid.Platform
{
    /// <summary>
    /// A user's profile with its editable attributes
    /// </summary>
    public class ProfileWithOTPVM : LightViewModel<ProfileWithOTPVM>
    {
        /// <summary>
        /// User's Id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// User's name 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// User's email
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// User's cellPhone
        /// </summary>
        public string Phone { get; set; }
        /// <summary>
        /// The OTP created
        /// </summary>
        public string OTP { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
        {
            RuleFor(i => false).Equal(true).WithError("This ViewModel type can only be used as response");
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
