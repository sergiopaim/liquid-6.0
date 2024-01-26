using FluentValidation;
using Liquid.Runtime;

namespace Liquid.OnAzure
{
    /// <summary>
    ///  Configuration of the for connect a Service Bus (Queue / Topic).
    /// </summary>
    public class ServiceBusConfiguration : LightConfig<ServiceBusConfiguration>
    {
        /// <summary>
        /// String of connection with the service bus defined on the azure.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        ///  The method used to properties validation of Configuration.
        /// </summary>
        public override void Validate()
        {
            RuleFor(d => ConnectionString).NotEmpty().WithError("ConnectionString settings should not be empty.");
            RuleFor(d => ConnectionString).Matches("Endpoint=sb://").WithError("No Endpoint on configuration string has been informed.");
            RuleFor(d => ConnectionString).Matches("SharedAccessKeyName=").WithError("No SharedAccessKeyName on configuration string has been informed.");
            RuleFor(d => ConnectionString).Matches("SharedAccessKey=").WithError("No SharedAccessKey on configuration string has been informed.");
        }
    }
}
