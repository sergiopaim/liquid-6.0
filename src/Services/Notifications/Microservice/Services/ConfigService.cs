using Liquid.Base;
using Liquid.Domain;
using Liquid.Platform;
using Microservice.Models;
using Microservice.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace Microservice.Services
{
    /// <summary>
    /// Manages the notification configuration for each user
    /// </summary>
    internal class ConfigService : LightService
    {
        internal async Task<Config> GetConfigById(string userId)
        {
            var userConfig = await Repository.GetByIdAsync<Config>(userId);

            if (userConfig is null)
            {
                //Tries to recover from async (queue based) race conditions from profile creation and notification subscription
                var notSyncedProfile = PlatformServices.GetUserProfile(this, userId);

                ResetNoContentError();

                if (notSyncedProfile is not null && !HasNoContentError && !HasBusinessErrors)
                {
                    await AddFromProfileAsync(notSyncedProfile);
                    userConfig = await Repository.GetByIdAsync<Config>(userId);
                }
            }

            return userConfig;
        }

        internal async Task<DomainResponse> AddFromProfileAsync(ProfileBasicVM profile)
        {
            Telemetry.TrackEvent("Add User Config from Profile", profile.Id);

            var userConfig = await Repository.GetByIdAsync<Config>(profile.Id) ?? new Config();

            userConfig.MapFrom(profile);
            userConfig.EmailChannel = new() { Email = profile.Email, IsValid = profile.EmailIsValid, NotificationTypes = NotificationType.GetAllCodes().ToList() };
            userConfig.PhoneChannel = new() { Phone = profile.Phone, IsValid = profile.PhoneIsValid, NotificationTypes = NotificationType.GetAllCodes().ToList() };
            userConfig.WebPushChannel = new() { NotificationTypes = NotificationType.GetAllCodes().ToList() };

            var upserted = await Repository.UpdateAsync(userConfig); //Actually does an Upsert

            var createdUserConfigVM = ConfigVM.FactoryFrom(upserted);
            createdUserConfigVM.Email = upserted.EmailChannel.Email;
            createdUserConfigVM.EmailIsValid = upserted.EmailChannel.IsValid;
            createdUserConfigVM.Phone = upserted.PhoneChannel.Phone;
            createdUserConfigVM.PhoneIsValid = upserted.PhoneChannel.IsValid;

            return Response(createdUserConfigVM);
        }

        internal async Task<DomainResponse> UpdateFromProfileAsync(ProfileBasicVM profile)
        {
            Telemetry.TrackEvent("Update User Config from Profile", profile.Id);

            var userConfig = await Repository.GetByIdAsync<Config>(profile.Id);

            if (userConfig is null)
                return NoContent();

            userConfig.MapFrom(profile);
            userConfig.EmailChannel.Email = profile.Email;
            userConfig.EmailChannel.IsValid = profile.EmailIsValid;
            userConfig.PhoneChannel.Phone = profile.Phone;
            userConfig.PhoneChannel.IsValid = profile.PhoneIsValid;

            var updatedUserConfig = await Repository.UpdateAsync(userConfig);

            var updatedUserConfigVM = ConfigVM.FactoryFrom(updatedUserConfig);
            updatedUserConfigVM.Email = updatedUserConfig.EmailChannel.Email;
            updatedUserConfigVM.EmailIsValid = updatedUserConfig.EmailChannel.IsValid;
            updatedUserConfigVM.Phone = updatedUserConfig.PhoneChannel.Phone;
            updatedUserConfigVM.PhoneIsValid = updatedUserConfig.PhoneChannel.IsValid;

            return Response(updatedUserConfigVM);
        }

        internal async Task<DomainResponse> DeleteFromProfileAsync(ProfileBasicVM profile)
        {
            Telemetry.TrackEvent("Update User Config from Profile", profile.Id);

            var userConfig = await Repository.GetByIdAsync<Config>(profile.Id);

            if (userConfig is null)
                return NoContent();

            await Service<NotificationService>().DeleteAllByUser(profile.Id);

            await Repository.DeleteAsync<Config>(profile.Id);

            await SendUserDeletionEmail(userConfig);

            return Response();
        }

        private async Task SendUserDeletionEmail(Config userConfig)
        {
            var msg = FactoryLightMessage<EmailMSG>(EmailCMD.Send);

            var formater = new FormatterByProfile(userConfig.Language, userConfig.TimeZone);

            formater.ApplyUserLanguage();

            msg.Email = userConfig.EmailChannel.Email;
            msg.Type = NotificationType.Account.Code;
            msg.Subject = LightLocalizer.Localize("PROFILE_DELETION_EMAIL_SUBJECT");
            msg.Message = LightLocalizer.Localize("PROFILE_DELETION_EMAIL_MESSAGE", "{HomeAppURL}/shutdown-policy");

            await Service<EmailService>().SendAsync(userConfig, msg);

            formater.RemoveUserLanguage();
        }
    }
}