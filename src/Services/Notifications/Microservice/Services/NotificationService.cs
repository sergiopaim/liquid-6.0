using Liquid;
using Liquid.Base;
using Liquid.Domain;
using Liquid.Platform;
using Microservice.Models;
using Microservice.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microservice.Services
{
    /// <summary>
    /// Sends contextual notifications thought user´s available channels
    /// </summary>
    internal class NotificationService : LightService
    {
        #region Migration

        internal async Task<DomainResponse> Migrate()
        {
            Telemetry.TrackEvent("Migrate Notifications");

            Console.WriteLine();
            Console.WriteLine("******** MIGRATING NOTIFICATIONS *******");

            List<string> ret = new();

            int updated = 0;

            foreach (var notif in Repository.GetAll<Notification>())
            {
                if (!notif.Cleared)
                {
                    await Repository.UpdateAsync(notif);

                    string sent = $"{++updated} Not cleared -> ({notif.Id})";
                    ret.Add(sent);

                    Console.WriteLine(sent);
                }
            }

            return Response(ret);
        }

        #endregion

        internal async Task<DomainResponse> GetMineById(string id)
        {
            Telemetry.TrackEvent("Get Notification", $"userId: {CurrentUserId} notificationId: {id}");

            var notification = await Repository.GetByIdAsync<Notification>(id, CurrentUserId);

            if (notification is null)
            {
                notification = Repository.Get<Notification>(n => n.Id == id).FirstOrDefault();

                if (notification is not null)
                {
                    var config = await Repository.GetByIdAsync<Config>(notification.UserId);
                    var login = config?.EmailChannel?.Email ?? config?.PhoneChannel?.Phone;
                    AddBusinessError("NOTIFICATION_OF_OTHER_USER", config?.Name);
                    AddBusinessInfo("OTHER_USER_LOGIN", login);

                    return Response();
                }

                return NoContent();
            }

            if (notification.Cleared)
                return NoContent();

            return Response(NotificationVM.FactoryFrom(notification));
        }

        internal DomainResponse GetAllMine()
        {
            Telemetry.TrackEvent("Get Notifications of Current User", CurrentUserId);

            var notifications = Repository.Get<Notification>(filter: a => a.UserId == CurrentUserId &&
                                                                          !a.Cleared,
                                                             orderBy: a => a.SentAt,
                                                             descending: true);

            return Response(notifications.Select(n => NotificationVM.FactoryFrom(n)));
        }

        internal DomainResponse GetAllByUser(string userId)
        {
            Telemetry.TrackEvent("Get Notifications of User", userId);

            var notifications = Repository.Get<Notification>(filter: a => a.UserId == userId,
                                                             orderBy: a => a.SentAt,
                                                             descending: true);

            return Response(notifications.Select(n => HistoryVM.FactoryFrom(n)));
        }

        internal async Task<DomainResponse> GetUserOfNotification(string id)
        {
            Telemetry.TrackEvent("Get User of Notification", id);

            //Making a generic query (not by partitionkey/id) because the partitionKey (userId) is indeed what is we are going to get
            var notif = (Repository.Get<Notification>(a => a.Id == id &&
                                                           !a.Cleared))
                                   .AsEnumerable()
                                   .FirstOrDefault();

            if (notif is null)
                return NoContent();

            var config = await Repository.GetByIdAsync<Config>(notif.UserId);

            return Response(BasicUserInfoVM.FactoryFrom(config));
        }

        internal async Task<DomainResponse> MarkAllMineAsViewed()
        {
            Telemetry.TrackEvent("Mark All Notification of Current User As Viewed", CurrentUserId);

            var viewedAt = WorkBench.UtcNow;
            var notViewedOnes = Repository.Get<Notification>(a => a.UserId == CurrentUserId &&
                                                                  a.ViewedAt == DateTime.MinValue &&
                                                                  !a.Cleared);

            List<NotificationVM> viewedOnes = new();
            foreach (var notViewed in notViewedOnes)
            {
                notViewed.ViewedAt = viewedAt;
                await Repository.UpdateAsync(notViewed);
                viewedOnes.Add(NotificationVM.FactoryFrom(notViewed));
            }

            return Response(viewedOnes);
        }

        internal async Task<DomainResponse> MarkMineAsViewedById(string notificationId)
        {
            Telemetry.TrackEvent("Mark Notification as Viewed", $"userId: {CurrentUserId} notificationId: {notificationId}");

            var toMarkAsViewed = await Repository.GetByIdAsync<Notification>(notificationId, CurrentUserId);

            if (toMarkAsViewed is null ||
                toMarkAsViewed.UserId != CurrentUserId ||
                toMarkAsViewed.Cleared)
                return NoContent();
            else
            {
                toMarkAsViewed.ViewedAt = WorkBench.UtcNow;
                var viewed = await Repository.UpdateAsync(toMarkAsViewed);

                return Response(NotificationVM.FactoryFrom(viewed));
            }
        }

        internal async Task<DomainResponse> ClearAllMineViewed()
        {
            Telemetry.TrackEvent("Clear All Viewed Notifications of Current User", CurrentUserId);

            var onesToClear = Repository.Get<Notification>(a => a.UserId == CurrentUserId &&
                                                                a.ViewedAt != DateTime.MinValue &&
                                                                !a.Cleared);

            List<NotificationVM> clearedOnes = new();
            foreach (var toClear in onesToClear)
            {
                toClear.Cleared = true;
                await Repository.UpdateAsync(toClear);
                clearedOnes.Add(NotificationVM.FactoryFrom(toClear));
            }

            return Response(clearedOnes);
        }

        internal async Task<DomainResponse> SendNotificationAsync(NotificationVM notifVM)
        {
            Telemetry.TrackEvent("Send Notification", notifVM.Id);

            var userConfig = await Service<ConfigService>().GetConfigById(notifVM.UserId);

            if (userConfig is null)
            {
                ResetNoContentError();
                return Response();
            }

            //Controls the fallback from WebPush channel to Email channel
            var immediateCount = await Service<WebPushService>().SendToAllEndPointsAsync(userConfig, notifVM, notifVM.Type);
            bool sendEmail = (immediateCount == 0);

            if (sendEmail)
            {
                var formatter = new FormatterByProfile(userConfig.Language, userConfig.TimeZone);

                formatter.ApplyUserLanguage();

                await SendByEmailAsync(userConfig, notifVM);

                formatter.RemoveUserLanguage();
            }
            var notifSavedVM = await SaveNotificationAsync(notifVM, sendEmail);

            if (HasBusinessErrors)
                return Response();

            return Response(NotificationVM.FactoryFrom(notifSavedVM));
        }

        internal async Task<DomainResponse> SendPushAsync(PushVM pushVM)
        {
            Telemetry.TrackEvent("Send Push", $"userId: {pushVM.UserId} contextUri:{pushVM.ContextUri}");

            var userConfig = await Service<ConfigService>().GetConfigById(pushVM.UserId);

            await Service<WebPushService>().SendToAllEndPointsAsync(userConfig, pushVM);

            return Response();
        }

        internal async Task<DomainResponse> RegisterNotificationAsync(NotificationVM notifVM)
        {
            Telemetry.TrackEvent("Register Notification", notifVM.Id);

            var notifSavedVM = await SaveNotificationAsync(notifVM);

            return Response(notifSavedVM);
        }

        internal async Task DeleteAllByUser(string userId)
        {
            Telemetry.TrackEvent("Delete All Notifications of User", userId);

            var onesToDelete = Repository.Get<Notification>(a => a.UserId == userId);

            foreach (var toDelete in onesToDelete)
            {
                await Repository.DeleteAsync<Notification>(toDelete.Id, userId);
            }
        }

        internal async Task ReinforceByEmailAsync(DateTime from, DateTime to)
        {
            Telemetry.TrackEvent("Reinforce By Email", $"from: {from} to: {to}");

            var onesToReinforce = Repository.Get<Notification>(a => !a.EmailSent &&
                                                                    a.ViewedAt == DateTime.MinValue &&
                                                                    (a.SentAt >= from && a.SentAt < to));
            foreach (var toReinforce in onesToReinforce)
            {
                var notifToReinforeVM = NotificationVM.FactoryFrom(toReinforce);

                var userConfig = await Service<ConfigService>().GetConfigById(toReinforce.UserId);
                var formatter = new FormatterByProfile(userConfig.Language, userConfig.TimeZone);

                formatter.ApplyUserLanguage();

                notifToReinforeVM.LongMessage = LightLocalizer.Localize("DID_YOU_SEE_THE_NOTIFICATION") + "> " + notifToReinforeVM.LongMessage;
                notifToReinforeVM.Target = toReinforce.Target;
                await SendByEmailAsync(userConfig, notifToReinforeVM);

                toReinforce.EmailSent = true;
                await Repository.UpdateAsync(toReinforce);

                formatter.RemoveUserLanguage();
            }

            return;
        }

        internal DomainResponse GetTypes()
        {
            Telemetry.TrackEvent("Get Status Types");

            return Response(NotificationType.GetAll().OrderBy(s => s.Label));
        }

        private async Task SendByEmailAsync(Config userConfig, NotificationVM notifVM)
        {
            var longMessage = EmailMSG.FactoryFrom(notifVM);
            longMessage.Subject = notifVM.ShortMessage;
            longMessage.Message = GetContextLinkFrom(notifVM.Id, notifVM.ShortMessage, notifVM.LongMessage, notifVM.Target);

            await Service<EmailService>().SendAsync(userConfig, longMessage);
        }

        private static string GetContextLinkFrom(string notificationId, string shortMessage, string longMessage, string target)
        {
            if (string.IsNullOrEmpty(longMessage))
            {
                longMessage = LightLocalizer.Localize("WE_HAVE_GOT_A_NEW_MESSAGE") + "> " + shortMessage + " ";
            }
            else
            {
                longMessage += ": ";
            }
            return
                longMessage + GetAppUrlFrom(target) + $"notifs/{notificationId}/read";
        }

        private static async Task<NotificationVM> SaveNotificationAsync(NotificationVM notifVM, bool sendEmail = false)
        {
            var notification = Notification.FactoryFrom(notifVM);
            notification.EmailSent = sendEmail;
            return NotificationVM.FactoryFrom(await Repository.AddAsync(notification));
        }

        private static Uri GetAppUrlFrom(string target)
        {
            if (target == NotificationTargetType.Prospect.Code)
                return PlatformServices.AppURLs["HomeAppURL"];
            else if (target == NotificationTargetType.Client.Code)
                return PlatformServices.AppURLs["ClientAppURL"];
            else if (target == NotificationTargetType.Staff.Code)
                return PlatformServices.AppURLs["EmployeeAppURL"];
            else
                return PlatformServices.AppURLs["MemberAppURL"];
        }
    }
}