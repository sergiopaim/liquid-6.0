using FluentValidation;
using Liquid;
using Liquid.Platform;
using Liquid.Repository;
using Liquid.Runtime;
using Microservice.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;

namespace Microservice.Models
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    //[AnalyticalSource]
    public class Profile : LightOptimisticModel<Profile>
    {
        private const int AAD_TOKEN_VALIDITY_IN_HOURS = 4;
        private const int IM_TOKEN_VALIDITY_IN_HOURS = 24;
        private string name;

        public string Name { get => name; set => name = value?.Trim()?.Replace("  ", " "); }
        public string Language { get; set; }
        public string TimeZone { get; set; }
        public Channels Channels { get; set; } = new();
        public JsonDocument UIPreferences { get; set; } = JsonDocument.Parse("{}");
        public List<Account> Accounts { get; set; } = new();
        public string Status { get; set; } = ProfileStatus.Active.Code;

        public override void Validate()
        {
            RuleFor(i => i.Name).NotEmpty().WithError("name must not be empty");
            RuleFor(i => i.Channels).NotEmpty().WithError("channels must not be empty");
            RuleFor(i => i.Status).Must(m => ProfileStatus.IsValid(m)).WithError("status is invalid");                
        }

        internal static Profile FactoryFromAADClaims(ClaimsPrincipal userClaims)
        {
            Profile factored = new()
            {
                Id = userClaims.FindFirstValue(JwtClaimTypes.UserId),
                Name = userClaims.FindFirstValue("given_name") + " " + userClaims.FindFirstValue("family_name"),

                Language = "pt",
                TimeZone = "America/Sao_Paulo"
            };

            if (string.IsNullOrEmpty(factored.name))
                factored.name = userClaims.FindFirstValue("name");

            factored.Channels.Email = userClaims.FindFirstValue("email")?.ToLower();
            factored.Channels.EmailIsValid = true;

            if (string.IsNullOrEmpty(factored.Channels.Email))
                factored.Channels.Email = userClaims.FindFirstValue("preferred_username")?.ToLower();

            factored.Accounts.Add(Account.FactoryFromAADClaims(userClaims));

            return factored;
        }

        internal void UpdateAADAccount(ClaimsPrincipal userClaims)
        {
            Accounts[0] = Account.FactoryFromAADClaims(userClaims);
        }

        internal static Profile FactoryFromAADUser(DirectoryUserSummaryVM user, List<string> roles)
        {
            if (user is null)
                return null;

            Profile factored = new()
            {
                Id = user.Id,
                Name = user.Name,

                Language = "pt",
                TimeZone = "America/Sao_Paulo"
            };

            if (!string.IsNullOrEmpty(user.Email))
            {
                factored.Channels.Email = user.Email?.ToLower();
                factored.Channels.EmailIsValid = true;
            }

            factored.Accounts.Add(Account.FactoryFromAADUser(user, roles));

            return factored;
        }

        internal static Profile FactoryMFrom(ProfileVM vm)
        {
            var m = FactoryFrom(vm);

            m.Channels.Email = vm.Email?.ToLower();
            m.Channels.EmailIsValid = vm.EmailIsValid;
            m.Channels.Phone = vm.Phone;
            m.Channels.PhoneIsValid = vm.PhoneIsValid;

            return m;
        }

        internal void MapFromEditVM(EditProfileVM vm)
        {
            MapFrom(vm);
            Channels.MapFromEditVM(vm);
        }

        internal ProfileVM FactoryVM()
        {
            var vm = ProfileVM.FactoryFrom(this);

            vm.Email = Channels.Email?.ToLower();
            vm.EmailIsValid = Channels.EmailIsValid && string.IsNullOrEmpty(Channels.EmailToChange);
            vm.Phone = Channels.Phone;
            vm.PhoneIsValid = Channels.PhoneIsValid && string.IsNullOrEmpty(Channels.PhoneToChange);

            vm.UIPreferences ??= JsonDocument.Parse("{}");

            vm.Roles = Accounts.SelectMany(a => a.Roles).Distinct().ToList();

            return vm;
        }

        internal ProfileWithPendingChangesVM FactoryWithPendingChangesVM()
        {
            var vm = ProfileWithPendingChangesVM.FactoryFrom(this);

            if (!string.IsNullOrEmpty(Channels.EmailToChange))
            {
                vm.Email = Channels.EmailToChange?.ToLower();
                vm.EmailIsValid = false;
            }
            else
            {
                vm.Email = Channels.Email?.ToLower();
                vm.EmailIsValid = Channels.EmailIsValid;
            }

            if (!string.IsNullOrEmpty(Channels.PhoneToChange))
            {
                vm.Phone = Channels.PhoneToChange;
                vm.PhoneIsValid = false;
            }
            else
            {
                vm.Phone = Channels.Phone;
                vm.PhoneIsValid = Channels.PhoneIsValid;
            }

            return vm;
        }

        internal ProfileWithOTPVM FactoryWithOTPVM()
        {
            var vm = ProfileWithOTPVM.FactoryFrom(this);

            vm.Email = Channels.Email?.ToLower();
            vm.Phone = Channels.Phone;

            vm.OTP = Accounts?.FirstOrDefault()?.Credentials?.OTP;

            return vm;
        }

        internal ProfileBasicVM FactoryBasicVM()
        {
            var vm = ProfileBasicVM.FactoryFrom(this);

            vm.Email = Channels.Email?.ToLower();
            vm.Phone = Channels.Phone;

            vm.Roles = Accounts.SelectMany(a => a.Roles).Distinct().ToList();

            return vm;
        }

        internal void MapToMSG(ProfileMSG msg)
        {
            msg.MapFrom(this);
            msg.Email = Channels.Email?.ToLower();
            msg.EmailIsValid = Channels.EmailIsValid;
            msg.Phone = Channels.Phone;
            msg.PhoneIsValid = Channels.PhoneIsValid;
            msg.Roles = Accounts.SelectMany(a => a.Roles).Distinct().ToList();
        }

        internal ComparableProfileVM CloneComparable()
        {
            ComparableProfileVM cloned = new()
            {
                Id = Id,
                Name = Name,
                Language = Language,
                TimeZone = TimeZone,
                Roles = Accounts.SelectMany(a => a.Roles).Distinct().ToList()
            };

            cloned.Channels.Email = Channels.Email?.ToLower();
            cloned.Channels.EmailIsValid = Channels.EmailIsValid;
            cloned.Channels.EmailToChange = Channels.EmailToChange?.ToLower();
            cloned.Channels.LastValidEmail = Channels.LastValidEmail?.ToLower();
            cloned.Channels.EmailOTP = Channels.EmailOTP;

            cloned.Channels.Phone = Channels.Phone;
            cloned.Channels.PhoneIsValid = Channels.PhoneIsValid;
            cloned.Channels.PhoneToChange = Channels.PhoneToChange;
            cloned.Channels.LastValidPhone = Channels.LastValidPhone;
            cloned.Channels.PhoneOTP = Channels.PhoneOTP;

            cloned.Channels.Initiated = Channels.Initiated;
            cloned.Channels.RevertOTP = Channels.RevertOTP;

            return cloned;
        }

        internal DateTime GetTokenExpiration()
        {
            if (Accounts.FirstOrDefault().Source == AccountSource.AAD.Code)
                return WorkBench.UtcNow.AddHours(AAD_TOKEN_VALIDITY_IN_HOURS);

            return WorkBench.UtcNow.AddHours(IM_TOKEN_VALIDITY_IN_HOURS);
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}