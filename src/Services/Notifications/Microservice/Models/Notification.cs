using FluentValidation;
using Liquid.Platform;
using Liquid.Repository;
using Liquid.Runtime;
using System;

namespace Microservice.Models
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class Notification : LightModel<Notification>
    {
        [PartitionKey]
        public string UserId { get; set; }
        public string Type { get; set; }
        public string Target { get; set; }
        public string ShortMessage { get; set; }
        public string LongMessage { get; set; }
        public string ContextUri { get; set; }
        public bool EmailSent { get; set; }
        public bool Cleared { get; set; } = false;
        public DateTime SentAt { get; set; }
        public DateTime ViewedAt { get; set; }

        public override void Validate()
        {
            RuleFor(i => i.Id).NotEmpty().WithError("id must not be empty");
            RuleFor(i => i.UserId).NotEmpty().WithError("userId must not be empty");
            RuleFor(i => i.Type).NotEmpty().Must(t => NotificationType.IsValid(t)).WithError("notificationType is invalid");
            RuleFor(i => i.ShortMessage).NotEmpty().WithError("shortMessage must not be empty");
            RuleFor(i => i.ShortMessage).MaximumLength(140).WithError("shortMessage must be up to 140 chars");
            RuleFor(i => i.SentAt).Must(s => s != DateTime.MinValue).WithError("sentAt must not be empty");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
