using Liquid.Base;
using Liquid.Domain;
using Liquid.Domain.API;
using Liquid.Interfaces;
using Liquid.OnAzure;
using Liquid.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Liquid.Platform
{
    /// <summary>
    /// Internal API for calling general plataform services (Profiles, Notifications and ReactiveHub)
    /// </summary>
    public static class PlatformServices
    {
        private static readonly GeneralConfig _value = LightConfigurator.LoadConfig<GeneralConfig>("General");
        private static readonly MessageBus<ServiceBus> userEmailsBus = new("TRANSACTIONAL", "user/emails");
        private static readonly MessageBus<ServiceBus> userTextBus = new("TRANSACTIONAL", "user/text");
        private static readonly MessageBus<ServiceBus> eventsDomainBus = new("TRANSACTIONAL", "events/domain");

        /// <summary>
        /// The list of Application URLs
        /// </summary>
        public static Dictionary<string, Uri> AppURLs => _value?.AppURLs;

        private const int NOTIFICATION_MINUTES_TO_LIVE = 60;

        /// <summary>
        /// Expands the 
        /// </summary>
        /// <param name="text">Text with app references in the format '{nameAppURL}'</param>
        /// <returns>Text with references expanded</returns>
        public static string ExpandAppUrls(string text)
        {
            foreach (var appUrl in AppURLs)
            {
                string url = appUrl.Value.ToString();

                if (url.Substring(url.Length - 1, 1) == "/")
                    url = url[0..^1];

                text = text.Replace("{" + appUrl.Key + "}", url, StringComparison.InvariantCulture);
            }

            return text;
        }

        /// <summary>
        /// Gets user´s profile data 
        /// </summary>
        /// <param name="lightDomain">The lightDomain calling class (this)</param>
        /// <param name="userId">The user´s Id</param>
        /// <returns>User´s profile data</returns>
        public static ProfileBasicVM GetUserProfile(ILightDomain lightDomain, string userId)
        {
            var profile = new LightApi("PROFILES", JwtSecurityCustom.Config.SysAdminJWT, lightDomain).Get<ProfileBasicVM>(userId);
            if (lightDomain.CritictHandler.HasNoContentError)
                lightDomain.CritictHandler.ResetNoContentError();

            return profile;
        }

        /// <summary>
        /// Gets user´s profile data 
        /// </summary>
        /// <param name="lightDomain">The lightDomain calling class (this)</param>
        /// <param name="email">The E-mail address</param>
        /// <returns>User´s profile data</returns>
        public static ProfileBasicVM GetUserProfileByEmail(ILightDomain lightDomain, string email)
        {
            var profile = new LightApi("PROFILES", JwtSecurityCustom.Config.SysAdminJWT, lightDomain).Get<ProfileBasicVM>($"byEmail/{email}");
            if (lightDomain.CritictHandler.HasNoContentError)
                lightDomain.CritictHandler.ResetNoContentError();

            return profile;
        }

        /// <summary>
        /// Gets basic user profiles by a list of ids
        /// </summary>
        /// <param name="lightDomain">The lightDomain calling class (this)</param>
        /// <param name="userIds">The list of ids</param>
        /// <returns>User´s profile data</returns>
        public static List<ProfileBasicVM> GetUserProfile(ILightDomain lightDomain, List<string> userIds)
        {
            const int REQUEST_BATCH = 50; //Avoids HTTP request overflow by a long query string
            var profilesAPI = new LightApi("PROFILES", JwtSecurityCustom.Config.SysAdminJWT, lightDomain);
            List<ProfileBasicVM> profiles = new();

            userIds = userIds.Distinct().Where(i => !string.IsNullOrEmpty(i)).ToList();

            for (int j = 0; j < userIds?.Count; j += REQUEST_BATCH)
            {
                var count = Math.Min(userIds.Count - j, REQUEST_BATCH);
                profiles.AddRange(profilesAPI.Get<List<ProfileBasicVM>>($"byIds?ids={string.Join("&ids=", userIds.GetRange(j, count))}"));
            }
            return profiles;
        }

        /// <summary>
        /// Gets basic backoffice user profiles with the informed backoffice role
        /// </summary>
        /// <param name="lightDomain">The lightDomain calling class (this)</param>
        /// <param name="backofficeRole">The backoffice role</param>
        /// <returns>User´s profile data</returns>
        public static List<ProfileVM> GetUserProfileByBackofficeRole(ILightDomain lightDomain, string backofficeRole)
        {
            return new LightApi("PROFILES", JwtSecurityCustom.Config.SysAdminJWT, lightDomain).Get<List<ProfileVM>>($"byRole/{backofficeRole}");
        }

        /// <summary>
        /// Gets basic backoffice user profiles with the informed backoffice role
        /// </summary>
        /// <param name="lightDomain">The lightDomain calling class (this)</param>
        /// <param name="backofficeRoles">The comma separated list of backoffice roles</param>
        /// <returns>User´s profile data</returns>
        public static List<ProfileVM> GetUserProfileByBackofficeRoles(ILightDomain lightDomain, string backofficeRoles)
        {
            var roles = backofficeRoles?.Split(",").Select(x => x.Trim()).ToList();
            return GetUserProfileByBackofficeRoles(lightDomain, roles);
        }

        /// <summary>
        /// Gets basic backoffice user profiles with the informed backoffice role
        /// </summary>
        /// <param name="lightDomain">The lightDomain calling class (this)</param>
        /// <param name="roles">The list of backoffice roles</param>
        /// <returns>User´s profile data</returns>
        public static List<ProfileVM> GetUserProfileByBackofficeRoles(ILightDomain lightDomain, List<string> roles)
        {
            if (!(roles?.Count > 0))
                return new();

            return new LightApi("PROFILES", JwtSecurityCustom.Config.SysAdminJWT, lightDomain).Get<List<ProfileVM>>($"byRoles?roles={string.Join("&roles=", roles)}");
        }

        /// <summary>
        /// Gets directory (AAD) users filtered by many parameters
        /// </summary>
        /// <param name="lightDomain">The lightDomain calling class (this)</param>
        /// <param name="tip">Tip to match the start of user names or emails</param>
        /// <param name="emailFilter">list of email ending parts to filter users for (ex: @gmail.com)</param>
        /// <param name="guestOnly">Indication whether only guest users should be returned (optional, false if not informed)</param>
        /// <returns>A summary directory user list</returns>
        public static List<DirectoryUserSummaryVM> FilterDirectoryUsers(ILightDomain lightDomain, string tip, List<string> emailFilter, bool guestOnly = false)
        {
            return new LightApi("PROFILES", JwtSecurityCustom.Config.SysAdminJWT, lightDomain)
                          .Get<List<DirectoryUserSummaryVM>>($"directory/filter?tip={tip}&emailFilter={string.Join("&emailFilter=", emailFilter)}&guestOnly={guestOnly}");
        }

        /// <summary>
        /// Invite user as directory (AAD) guest users
        /// </summary>
        /// <param name="lightDomain">The lightDomain calling class (this)</param>
        /// <param name="name">The name of the user</param>
        /// <param name="email">The user email address to invite the user and to be used as an alternate key</param>
        /// <param name="role">The initial role the user is going to have</param>
        /// <param name="redirectUrl">The url to redirect the user after redeem process</param>
        /// <returns>A user invitation data</returns>
        public static DirectoryUserInvitationVM InviteUserToDirectory(ILightDomain lightDomain, string name, string email, string role, string redirectUrl)
        {
            return new LightApi("PROFILES", JwtSecurityCustom.Config.SysAdminJWT, lightDomain)
                          .Post<DirectoryUserInvitationVM>($"directory/invite?name={name}&email={email}&role={role}&redirectUrl={redirectUrl}");
        }

        /// <summary>
        /// Updates the a directory user (AAD) roles
        /// </summary>
        /// <param name="lightDomain">The lightDomain calling class (this)</param>
        /// <param name="id">User id</param>
        /// <param name="roles">The list of roles the user has</param>
        /// <returns>A user profile</returns>
        public static ProfileVM UpdateDirectoryUserRoles(ILightDomain lightDomain, string id, List<string> roles)
        {
            return new LightApi("PROFILES", JwtSecurityCustom.Config.SysAdminJWT, lightDomain)
                          .Put<ProfileVM>($"directory/{id}/roles?roles={string.Join("&roles=", roles)}");
        }

        /// <summary>
        /// Creates or updates a user profile and returns it with a (first) OTP code for initial authentication
        /// </summary>
        /// <param name="lightDomain">The lightDomain calling class (this)</param>
        /// <param name="user">The user profile data</param>
        /// <returns>User´s profile data wit a OTP code</returns>
        public static ProfileWithOTPVM CreateOrUpdateUser(ILightDomain lightDomain, ProfileVM user)
        {
            return new LightApi("PROFILES", JwtSecurityCustom.Config.SysAdminJWT, lightDomain).Post<ProfileWithOTPVM>("", user.ToJsonDocument());
        }

        /// <summary>
        /// Create service account user 
        /// </summary>
        /// <param name="lightDomain">The lightDomain calling class (this)</param>
        /// <param name="userId">The id of the service account</param>
        /// <param name="name">The name of the service account</param>
        /// <param name="email">The (admin) email of the service account</param>
        /// <returns>The service account user credentials</returns>
        public static ServiceAccountVM CreateServiceAccount(ILightDomain lightDomain, string userId, string name, string email)
        {
            return new LightApi("PROFILES", JwtSecurityCustom.Config.SysAdminJWT, lightDomain).Post<ServiceAccountVM>($"serviceAccount/{userId}?name={name}&email={email}");
        }

        /// <summary>
        /// Updates an existing service account user secret
        /// </summary>
        /// <param name="lightDomain">The lightDomain calling class (this)</param>
        /// <param name="userId">The id of the service account</param>
        /// <param name="name">The name of the service account</param>
        /// <param name="email">The (admin) email of the service account</param>
        /// <returns>The service account user credentials</returns>
        public static ServiceAccountVM UpdateServiceAccount(ILightDomain lightDomain, string userId, string name, string email = null)
        {
            string query = $"serviceAccount/{userId}?name={name}";

            if (!string.IsNullOrEmpty(email))
                query += $"&email={email}";

            return new LightApi("PROFILES", JwtSecurityCustom.Config.SysAdminJWT, lightDomain).Put<ServiceAccountVM>(query);
        }

        /// <summary>
        /// Deletes an existing service account
        /// </summary>
        /// <param name="lightDomain">The lightDomain calling class (this)</param>
        /// <param name="userId">The id of the service account</param>
        /// <returns>The deleted service account</returns>
        public static ServiceAccountVM DeleteServiceAccount(ILightDomain lightDomain, string userId)
        {
            string query = $"serviceAccount/{userId}";

            return new LightApi("PROFILES", JwtSecurityCustom.Config.SysAdminJWT, lightDomain).Delete<ServiceAccountVM>(query);
        }

        /// <summary>
        /// Generates a new service account user secret
        /// </summary>
        /// <param name="lightDomain">The lightDomain calling class (this)</param>
        /// <param name="userId">The id of the service account user</param>
        /// <returns>The service account user credentials</returns>
        public static ServiceAccountVM GenerateServiceAccountSecret(ILightDomain lightDomain, string userId)
        {
            return new LightApi("PROFILES", JwtSecurityCustom.Config.SysAdminJWT, lightDomain).Put<ServiceAccountVM>($"serviceAccount/{userId}/newSecret");
        }

        /// <summary>
        /// Generates a new OTP code for the user
        /// </summary>
        /// <param name="lightDomain">The lightDomain calling class (this)</param>
        /// <param name="userId">The user´s Id</param>
        /// <returns>A new OTP code for the user</returns>
        public static ProfileWithOTPVM GetNewOTPForUser(ILightDomain lightDomain, string userId)
        {
            return new LightApi("PROFILES", JwtSecurityCustom.Config.SysAdminJWT, lightDomain).Get<ProfileWithOTPVM>($"auth/otp?accountId={userId}");
        }

        /// <summary>
        /// Sends an Email message to the user
        /// </summary>
        /// <param name="emailMSG">The email message</param>
        public static void SendEmail(EmailMSG emailMSG)
        {
            userEmailsBus.SendToQueueAsync(emailMSG, NOTIFICATION_MINUTES_TO_LIVE).Wait();
        }

        /// <summary>
        /// Sends a text message to the user
        /// </summary>
        /// <param name="textMSG">The text message</param>
        public static void SendText(ShortTextMSG textMSG)
        {
            userTextBus.SendToQueueAsync(textMSG, NOTIFICATION_MINUTES_TO_LIVE).Wait();
        }

        /// <summary>
        /// Sends a domain event to users via ReactiveHub
        /// </summary>
        /// <param name="domainEventMSG">The domain message</param>
        public static void SendDomainEvent(DomainEventMSG domainEventMSG)
        {
            eventsDomainBus.SendToTopicAsync(domainEventMSG).Wait();
        }

        /// <summary>
        /// Sends an Notification message to the user
        /// </summary>
        /// <param name="lightDomain">The lightDomain calling class (this)</param>
        /// <param name="notifVM">The notification message</param>
        public static NotificationVM SendNotification(ILightDomain lightDomain, NotificationVM notifVM)
        {
            if (notifVM is null)
            {
                throw new LightException("CANNOT_SEND_A_NULL_NOTIFICATION");
            }

            notifVM.Id = null; //Clears any id eventually sent via (the general) NotificationVM
            return new LightApi("REACTIVEHUB", JwtSecurityCustom.Config.SysAdminJWT, lightDomain).Post<NotificationVM>("notify", notifVM.ToJsonDocument());
        }
    }
}