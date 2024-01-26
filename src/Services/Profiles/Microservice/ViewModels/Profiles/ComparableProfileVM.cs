using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;
using Liquid.Platform;
using Microservice.Models;
using System.Collections.Generic;

namespace Microservice.ViewModels
{
    /// <summary>
    /// A user's profile used for compare update changes
    /// </summary>
    public class ComparableProfileVM : LightViewModel<ComparableProfileVM>
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
        /// User's roles from all accounts
        /// </summary>
        public List<string> Roles { get; set; } = new();

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public Channels Channels { get; set; } = new();

        public override void Validate()
        {
            RuleFor(i => false).Equal(true).WithError("This ViewModel type can only be used as response");
        }

        internal void MapToMSG(ProfileMSG msg)
        {
            msg.MapFrom(this);
            msg.Email = Channels.Email?.ToLower();
            msg.EmailIsValid = Channels.EmailIsValid;
            msg.Phone = Channels.Phone;
            msg.PhoneIsValid = Channels.PhoneIsValid;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
