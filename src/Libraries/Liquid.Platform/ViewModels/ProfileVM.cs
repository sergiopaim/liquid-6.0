using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;
using System.Collections.Generic;
using System.Text.Json;

namespace Liquid.Platform
{
    /// <summary>
    /// A user's profile with its editable attributes
    /// </summary>
    public class ProfileVM : LightViewModel<ProfileVM>
    {
        /// <summary>
        /// User's id
        /// </summary>
        public string Id { get; set; }
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
        /// Profile property containng arbitrary object for the use of apps to store user UI preferences
        /// </summary>
        public JsonDocument UIPreferences { get; set; }
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

        /// <summary>
        /// User's roles from all accounts
        /// </summary>
        public List<string> Roles { get; set; } = new();

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
        {
            RuleFor(i => i.Id).NotEmpty().WithError("id must not be empty");
            RuleFor(i => i.Email).NotEmpty().EmailAddress().WithError("email is invalid"); ;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
