using Liquid.Domain;

namespace Liquid.Platform
{
    /// <summary>
    /// Type of notifications sent to users
    /// </summary>
    public class NotificationType : LightLocalizedEnum<NotificationType>
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static readonly NotificationType Account = new(nameof(Account));
        public static readonly NotificationType Direct = new(nameof(Direct));
        public static readonly NotificationType Tasks = new(nameof(Tasks));
        public static readonly NotificationType Marketing = new(nameof(Marketing));

        public NotificationType(string code) : base(code) { }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}