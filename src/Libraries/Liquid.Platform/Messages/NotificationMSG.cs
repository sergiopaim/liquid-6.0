using FluentValidation;
using Liquid.Activation;
using Liquid.Domain;
using Liquid.Runtime;
using System;

namespace Liquid.Platform
{

    /// <summary>
    /// Type of commands the notification message carries on
    /// </summary>
    public class NotificationCMD : LightEnum<NotificationCMD>
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static readonly NotificationCMD Send = new(nameof(Send));
        public static readonly NotificationCMD Register = new(nameof(Register));

        public NotificationCMD(string code) : base(code) { }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

    /// <summary>
    /// MessageBus message type to process notification async
    /// </summary>
    public class NotificationMSG : LightMessage<NotificationMSG, NotificationCMD>
    {
        /// <summary>
        /// Notification's id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// User's id
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// Type of notification
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Target type of the notification
        /// </summary>
        public string Target { get; set; } = NotificationTargetType.Member.Code;
        /// <summary>
        /// Short message (up to 140 chars) to be promptly shown to the user
        /// </summary>
        public string ShortMessage { get; set; }
        /// <summary>
        /// Long message to be shown to the user as further detail
        /// </summary>
        public string LongMessage { get; set; }
        /// <summary>
        /// Relative URI to solve/open the notification
        /// </summary>
        public string ContextUri { get; set; }
        /// <summary>
        /// DataTime at which the notification was sent
        /// </summary>
        public DateTime SentAt { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
        {
            RuleFor(i => i.Id).NotEmpty().WithError("id must not be empty");
            RuleFor(i => i.UserId).NotEmpty().WithError("userId must not be empty");
            RuleFor(i => i.Type).NotEmpty().Must(t => NotificationType.IsValid(t)).WithError("notificationType is invalid");
            RuleFor(i => i.Target).NotEmpty().Must(t => NotificationTargetType.IsValid(t)).WithError("target is invalid");
            RuleFor(i => i.ShortMessage).NotEmpty().WithError("shortMessage must not be empty");
            RuleFor(i => i.ShortMessage).MaximumLength(140).WithError("shortMessage must be up to 140 chars");
            RuleFor(i => i.SentAt).Must(s => s != DateTime.MinValue).WithError("sentAt must not be empty");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}