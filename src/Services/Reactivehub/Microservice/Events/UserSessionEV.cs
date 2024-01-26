using Liquid.Activation;
using System;

namespace Microservice.Events
{
#pragma warning disable CA1056 // Uri properties should not be strings
    /// <summary>
    /// Reactive Event indicating that a notification was sent to the user
    /// </summary>
    public class UserSessionEV : LightReactiveEvent<UserSessionEV> {

        /// <summary>
        /// User's session id
        /// </summary>
        public string SessionId { get; set; }
        /// <summary>
        /// Type of UI Event
        /// </summary>
        public string UIEvent { get; set; }
        /// <summary>
        /// Event context
        /// </summary>
        public string Context { get; set; }
        /// <summary>
        /// Short message to be promptly shown to user
        /// </summary>

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate() { }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    }
#pragma warning restore CA1056 // Uri properties should not be strings
}