using FluentValidation;
using Liquid.Activation;
using Liquid.Domain;
using Liquid.Runtime;

namespace Liquid.Platform
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class ShortTextCMD : LightEnum<ShortTextCMD>
    {
        public static readonly ShortTextCMD Send = new(nameof(Send));

        public ShortTextCMD(string code) : base(code) { }
    }

    /// <summary>
    /// A short (text) message to user
    /// </summary>
    public class ShortTextMSG : LightMessage<ShortTextMSG, ShortTextCMD>
    {
        /// <summary>
        /// User's id
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// Destiny phone number (optional - if missed, the text message will be sent to user's phone number
        /// </summary>
        public string Phone { get; set; }
        /// <summary>
        /// Type of notification
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// The body of the message to user
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Indication whether the 'Sender' company be added to the message
        /// </summary>
        public bool ShowSender { get; set; } = true;

        public override void Validate()
        {
            if (Type != NotificationType.Direct.Code)
                RuleFor(i => i.UserId).NotEmpty().WithError("userId must not be empty");

            RuleFor(i => i.Message).NotEmpty().WithError("message must not be empty");
            RuleFor(i => i.Type).Must(t => NotificationType.IsValid(t)).WithError("notificationType is invalid");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}