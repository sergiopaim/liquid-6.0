using FluentValidation;
using Liquid.Activation;
using Liquid.Domain;
using Liquid.Runtime;
using System.Collections.Generic;

namespace Liquid.Platform
{
    /// <summary>
    /// Type of commands the profile message carries on
    /// </summary>
    public class ProfileCMD : LightEnum<ProfileCMD>
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static readonly ProfileCMD Create = new(nameof(Create));
        public static readonly ProfileCMD Update = new(nameof(Update));
        public static readonly ProfileCMD Delete = new(nameof(Delete));

        public ProfileCMD(string code) : base(code) { }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    }

    /// <summary>
    /// Message to notify other microservices of changes in user's profile
    /// </summary>
    public class ProfileMSG : LightMessage<ProfileMSG, ProfileCMD>
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
            RuleFor(i => i.Language).Must(l => string.IsNullOrEmpty(l) || LanguageType.IsValid(l)).WithError("language is invalid");
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
