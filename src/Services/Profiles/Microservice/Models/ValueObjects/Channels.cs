using FluentValidation;
using Liquid.Domain;
using Liquid.Repository;
using Liquid.Runtime;
using Microservice.ViewModels;
using System;

namespace Microservice.Models
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class Channels : LightValueObject<Channels>
    {
        public string Email { get; set; }
        public bool EmailIsValid { get; set; }
        public string EmailToChange { get; set; }
        public string LastValidEmail { get; set; }
        public string EmailOTP { get; set; }

        public string Phone { get; set; }
        public bool PhoneIsValid { get; set; }
        public string PhoneToChange { get; set; }
        public string LastValidPhone { get; set; }
        public string PhoneOTP { get; set; }

        public bool Initiated { get; set; }
        public string RevertOTP { get; set; }
        public DateTime ReversableUntil { get; set; }

        public override void Validate() 
        {
            RuleFor(i => i.Phone).Must(p => PhoneNumber.IsNullOrEmptyOrValid(p)).WithError("invalid phone number");
            RuleFor(i => i.PhoneToChange).Must(p => PhoneNumber.IsNullOrEmptyOrValid(p)).WithError("invalid phoneToChange number");
            RuleFor(i => i.LastValidPhone).Must(p => PhoneNumber.IsNullOrEmptyOrValid(p)).WithError("invalid lastValidPhone number");

            RuleFor(i => i.Email).Must(e => EmailAddress.IsNullOrEmptyOrValid(e)).WithError("invalid email address");
            RuleFor(i => i.EmailToChange).Must(e => EmailAddress.IsNullOrEmptyOrValid(e)).WithError("invalid emailToChange address");
            RuleFor(i => i.LastValidEmail).Must(e => EmailAddress.IsNullOrEmptyOrValid(e)).WithError("invalid lastValidEmail address");
        }

        public void MarkAsValid(string channelType)
        {
            if (channelType == ChannelType.Email.Code)
                EmailIsValid = true;
            else if (channelType == ChannelType.Phone.Code)
                PhoneIsValid = true;

            Initiated = true;
        }

        internal bool WillChangeFrom(EditProfileVM vm)
        {
            return !(Email == vm.Email?.ToLower()) ||
                   !(Phone == vm.Phone);
        }

        internal void MapFromEditVM(EditProfileVM vm)
        {
            Email = vm.Email?.ToLower();
            Phone = vm.Phone;
        }

        internal bool WillRemoveAnyFrom(EditProfileVM vm)
        {
            return (!string.IsNullOrEmpty(Email) && string.IsNullOrEmpty(vm.Email)) ||
                   (!string.IsNullOrEmpty(Phone) && string.IsNullOrEmpty(vm.Phone));
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}

