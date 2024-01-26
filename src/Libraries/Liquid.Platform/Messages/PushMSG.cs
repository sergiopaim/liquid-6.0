using FluentValidation;
using Liquid.Activation;
using Liquid.Domain;
using Liquid.Runtime;

namespace Liquid.Platform
{

    /// <summary>
    /// Type of commands the push notification message carries on
    /// </summary>
    public class PushCMD : LightEnum<PushCMD>
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static readonly PushCMD Send = new(nameof(Send));

        public PushCMD(string code) : base(code) { }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

    /// <summary>
    /// MessageBus message type to process push notification async
    /// </summary>
    public class PushMSG : LightMessage<PushMSG, PushCMD>
    {
        /// <summary>
        /// User's id
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Short message (up to 140 chars) to be promptly shown to the user
        /// </summary>
        public string ShortMessage { get; set; }
        /// <summary>
        /// Relative URI to open the event/do the action
        /// </summary>
        public string ContextUri { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override void Validate()
        {
            RuleFor(i => i.UserId).NotEmpty().WithError("userId must not be empty");
            RuleFor(i => i.ShortMessage).NotEmpty().WithError("shortMessage must not be empty");
            RuleFor(i => i.ShortMessage).MaximumLength(140).WithError("shortMessage must be up to 140 chars");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}