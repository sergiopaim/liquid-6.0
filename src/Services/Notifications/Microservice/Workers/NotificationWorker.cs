using Liquid.Activation;
using Liquid.Platform;
using Microservice.Services;
using System.Threading.Tasks;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Microservice.Workers
{
    [MessageBus("TRANSACTIONAL")]
    public class NotificationWorker : LightWorker
    {
        [Topic("user/profiles", "notifications", maxConcurrentCalls: 1, deleteAfterRead: false)]
        public async Task ProcessProfileMessageAsync(ProfileMSG profileMSG)
        {
            ValidateInput(profileMSG);
            var profileVM = ProfileBasicVM.FactoryFrom(profileMSG);

            if (profileMSG.CommandType == ProfileCMD.Create.Code)
                await Factory<ConfigService>().AddFromProfileAsync(profileVM);

            if (profileMSG.CommandType == ProfileCMD.Update.Code)
                await Factory<ConfigService>().UpdateFromProfileAsync(profileVM);

            if (profileMSG.CommandType == ProfileCMD.Delete.Code)
                await Factory<ConfigService>().DeleteFromProfileAsync(profileVM);

            Terminate();
        }

        [Queue("user/emails", maxConcurrentCalls: 1, deleteAfterRead: false)]
        public async Task ProcessEmailMessagesAsync(EmailMSG emailMSG)
        {
            ValidateInput(emailMSG);

            if (emailMSG.CommandType == EmailCMD.Send.Code)
                await Factory<EmailService>().SendAsync(emailMSG);

            Terminate();
        }

        [Queue("user/text", maxConcurrentCalls: 1, deleteAfterRead: false)]
        public async Task ProcessTextMessagesAsync(ShortTextMSG textMSG)
        {
            ValidateInput(textMSG);

            if (textMSG.CommandType == ShortTextCMD.Send.Code)
                await Factory<TextService>().SendAsync(textMSG);

            Terminate();
        }

        [Queue("user/notifs", maxConcurrentCalls: 1, deleteAfterRead: false)]
        public async Task ProcessNotificationMessageAsync(NotificationMSG notifMSG)
        {
            ValidateInput(notifMSG);
            var notifVM = NotificationVM.FactoryFrom(notifMSG);

            if (notifMSG.CommandType == NotificationCMD.Send.Code)
                await Factory<NotificationService>().SendNotificationAsync(notifVM);
            else if (notifMSG.CommandType == NotificationCMD.Register.Code)
                await Factory<NotificationService>().RegisterNotificationAsync(notifVM);

            Terminate();
        }

        [Queue("user/pushes", maxConcurrentCalls: 1, deleteAfterRead: false)]
        public async Task ProcessPushMessageAsync(PushMSG pushMSG)
        {
            ValidateInput(pushMSG);

            if (pushMSG.CommandType == PushCMD.Send.Code)
                await Factory<NotificationService>().SendPushAsync(PushVM.FactoryFrom(pushMSG));

            Terminate();
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}