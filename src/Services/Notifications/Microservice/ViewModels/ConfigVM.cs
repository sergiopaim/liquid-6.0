using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;

namespace Microservice.ViewModels
{
    /// <summary>
    /// A user's configuration profile with its exposable attributes
    /// </summary>
    public class ConfigVM : LightViewModel<ConfigVM>
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
        /// The user's phone
        /// </summary>
        public string Phone { get; set; }
        /// <summary>
        /// Indicates whether the phone has been validated
        /// </summary>
        public bool PhoneIsValid { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
        {
            RuleFor(i => i.Id).NotEmpty().WithError("id must not be empty");
            RuleFor(i => i.Email).NotEmpty().EmailAddress().WithError("email is invalid");;
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }  
}