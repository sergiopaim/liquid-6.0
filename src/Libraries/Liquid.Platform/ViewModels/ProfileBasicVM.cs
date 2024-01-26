using Liquid.Domain;
using System.Collections.Generic;

namespace Liquid.Platform
{
    /// <summary>
    /// A user's profile basic attributes
    /// </summary>
    public class ProfileBasicVM : LightViewModel<ProfileBasicVM>
    {
        /// <summary>
        /// Id of the user
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
        /// The user's email address
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
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
