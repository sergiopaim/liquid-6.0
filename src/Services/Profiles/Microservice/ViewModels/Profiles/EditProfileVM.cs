using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;
using System.Text.Json;

namespace Microservice.ViewModels
{
    /// <summary>
    /// A user's profile with its editable attributes
    /// </summary>
    public class EditProfileVM : LightViewModel<EditProfileVM>
    {
        private string email;

        /// <summary>
        /// User´s name 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Language selected by the user
        /// </summary>
        public string Language { get; set; }
        /// <summary>
        /// Timezone selected by the user
        /// </summary>
        public string TimeZone { get; set; }
        /// <summary>
        /// The user's email address
        /// </summary>
        public string Email { get => email; set => email = value?.ToLower().Trim(); }
        /// <summary>
        /// The user's phone number
        /// </summary>
        public string Phone { get; set; }
        /// <summary>
        /// Profile property containing arbitrary object for the use of apps to store user UI preferences
        /// </summary>
        public JsonDocument UIPreferences { get; set; } = JsonDocument.Parse("{}");

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate() 
        {
            RuleFor(i => i.Name).NotEmpty().WithError("name must not be empty");
            RuleFor(i => i.Email).Must(e => EmailAddress.IsNullOrEmptyOrValid(e)).WithError("invalid email address");
            RuleFor(i => i.Phone).Must(p => PhoneNumber.IsNullOrEmptyOrValid(p)).WithError("invalid phone number");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
