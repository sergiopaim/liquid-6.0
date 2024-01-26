using Liquid.Activation;
using System;

namespace Microservice.Events
{
    /// <summary>
    /// Reactive Event indicating that a notification was sent to the user
    /// </summary>
    public class NotificationEV : LightReactiveEvent<NotificationEV> {

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
        /// Short message to be promptly shown to user
        /// </summary>
        public string ShortMessage { get; set; }
        /// <summary>
        /// Long message to be shown to user as further detail
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
        /// <summary>
        /// DataTime at which the notification was viewed by the user
        /// </summary>
        public DateTime ViewedAt { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate() { }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    }
}