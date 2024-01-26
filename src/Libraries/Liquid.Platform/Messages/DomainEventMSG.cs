using FluentValidation;
using Liquid.Activation;
using Liquid.Domain;
using Liquid.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Liquid.Platform
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
{
    /// <summary>
    /// Type of commands the domain event message carries on
    /// </summary>
    public class DomainEventCMD : LightEnum<DomainEventCMD>
    {
        public static readonly DomainEventCMD Notify = new(nameof(Notify));

        public DomainEventCMD(string code) : base(code) { }
    }

    /// <summary>
    /// Message to notify application logic of domain events
    /// </summary>
    public class DomainEventMSG : LightMessage<DomainEventMSG, DomainEventCMD>
    {
        /// <summary>
        /// ReactiveHub anonymous connection ids to notify
        /// </summary>
        public List<string> AnonConns { get; set; } = new();
        /// <summary>
        /// List of user ids to notify
        /// </summary>
        public List<string> UserIds { get; set; } = new();
        /// <summary>
        /// User´s name 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Short message for user notification
        /// </summary>
        public string ShortMessage { get; set; }
        /// <summary>
        /// Indication whether the domainEvent should be sent via WebPush if the app if offline
        /// </summary>
        public bool PushIfOffLine { get; set; }
        /// <summary>
        /// Generic payload object
        /// </summary>
        public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");

        public override void Validate()
        {
            RuleFor(i => i.UserIds).Must(ids => (ids.Count > 0 && !ids.Any(id => string.IsNullOrEmpty(id))) ||
                                                (AnonConns.Count > 0 && !AnonConns.Any(conn => string.IsNullOrEmpty(conn))))
                                   .WithError("either userIds or anonConns must not be empty");

            RuleFor(i => i.Name).NotEmpty().WithError("name must not be empty");
            RuleFor(i => i.ShortMessage).NotEmpty().WithError("shortMessage must not be empty");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
