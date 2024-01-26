using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;
using System;

namespace Microservice.ViewModels
{
    /// <summary>
    /// A notification history
    /// </summary>
    public class HistoryVM : LightViewModel<HistoryVM>
    {
        /// <summary>
        /// Notification's id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Short message to be promptly shown to user
        /// </summary>
        public string ShortMessage { get; set; }
        /// <summary>
        /// Long message to be shown to user as further detail
        /// </summary>
        public string LongMessage { get; set; }
        /// <summary>
        /// DataTime at which the notification was sent
        /// </summary>
        public DateTime SentAt { get; set; }
        /// <summary>
        /// DataTime at which the notification was viewed by the user
        /// </summary>
        public DateTime ViewedAt { get; set; }
        /// <summary>
        /// Indication whether an reinforment by email was sent
        /// </summary>
        public bool EmailSent { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
        {
            RuleFor(i => false).Equal(true).WithError("This ViewModel type can only be used as response");
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}