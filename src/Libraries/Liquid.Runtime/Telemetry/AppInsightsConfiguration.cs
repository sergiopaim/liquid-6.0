using FluentValidation;

namespace Liquid.Runtime
{
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// AppInsights Configurator will dynamically assign the authorization key for writing to AppInsights.
    /// The function caller should pass a configuration file containing the Azure key.
    /// </summary>
    internal class AppInsightsConfiguration : LightConfig<AppInsightsConfiguration>
    {
        //Duplicated with Liquid.OnAzure.Telemetry due to a non generalization of Runtime.Telemetry for many providers (such as AddTelemetry<T>()).

        /// <summary>
        /// Necessary key for sends data to telemetry. Otherwise no data will tracked.
        /// </summary>
        public string InstrumentationKey { get; set; }
        /// <summary>
        /// Necessary key for enabling live metrics.
        /// </summary>
        public string QuickPulseApiKey { get; set; }
        public bool EnableKubernetes { get; set; }
        public override void Validate()
        {
            RuleFor(d => InstrumentationKey).NotEmpty().WithError("instrumentationKey must not be empty");
            RuleFor(d => QuickPulseApiKey).NotEmpty().WithError("quickPulseApiKey must not be empty");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore IDE0079 // Remove unnecessary suppression
}
