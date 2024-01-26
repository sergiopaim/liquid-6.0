using FluentValidation;
using Liquid.Activation;
using Liquid.Domain;
using Liquid.Runtime;

namespace Liquid.Platform
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class EmailCMD : LightEnum<EmailCMD>
    {
        public static readonly EmailCMD Send = new(nameof(Send));

        public EmailCMD(string code) : base(code) { }
    }

    /// <summary>
    /// An E-mail message to the user
    /// </summary>
    public class EmailMSG : LightMessage<EmailMSG, EmailCMD>
    {
        /// <summary>
        /// User's id
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// Destiny email address (optional - if missed, the message will be sent to user's e-mail address
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// Type of notification
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Subject of the message to user
        /// </summary>
        public string Subject { get; set; }
        /// <summary>
        /// The body of the message to user
        /// </summary>
        public string Message { get; set; }

        public override void Validate()
        {
            if (Type != NotificationType.Direct.Code)
                RuleFor(i => i.UserId).NotEmpty().WithError("userId must not be empty");
            RuleFor(i => i.Subject).NotEmpty().WithError("subject must not be empty");
            RuleFor(i => i.Message).NotEmpty().WithError("message must not be empty");
            RuleFor(i => i.Type).Must(t => NotificationType.IsValid(t)).WithError("notificationType is invalid");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}