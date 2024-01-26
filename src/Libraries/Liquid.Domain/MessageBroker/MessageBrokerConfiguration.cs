using FluentValidation;
using Liquid.Runtime;

namespace Liquid.Domain
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class MessageBrokerConfiguration : LightConfig<MessageBrokerConfiguration>
    {
        public string ConnectionString { get; set; }

        public override void Validate()
        {
            RuleFor(d => ConnectionString).NotEmpty().WithError("ConnectionString settings should not be empty.");
            RuleFor(d => ConnectionString).Matches("Endpoint=sb://").WithError("No Endpoint on configuration string has been informed.");
            RuleFor(d => ConnectionString).Matches("SharedAccessKeyName=").WithError("No SharedAccessKeyName on configuration string has been informed.");
            RuleFor(d => ConnectionString).Matches("SharedAccessKey=").WithError("No SharedAccessKey on configuration string has been informed.");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
