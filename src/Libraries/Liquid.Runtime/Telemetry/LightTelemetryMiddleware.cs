using Liquid.OnAzure.Telemetry;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Liquid.Runtime.Telemetry
{
    /// <summary>
    /// Add some methods to configure Telemetry providers.
    /// </summary>
    public static class TelemetryExtensions
    {
        /// <summary>
        /// Enable Application Insights telemetry
        /// </summary>
        /// <param name="services">Collection of services that the telemetry will be added to</param>
        /// <remarks>
        /// Only works if the configuration file contains a section named 'ApplicationInsights'.
        /// Will also add the KubernetesEnricher if the section contains a key EnableKubernetes with 
        /// value 'true'.
        /// </remarks>
        public static void AddTelemetry(this IServiceCollection services)
        {
            AppInsightsConfiguration applicationInsightsSection = LightConfigurator.LoadConfig<AppInsightsConfiguration>("ApplicationInsights");

            if (applicationInsightsSection is null) return;

            ApplicationInsightsServiceOptions options = new()
            {
                //Track dependency failures as bellow
                //EnableDependencyTrackingTelemetryModule = false,
                InstrumentationKey = applicationInsightsSection.InstrumentationKey
            };
            //Process only dependencies failures
            services.AddApplicationInsightsTelemetryProcessor<LightTelemetryDependencyFilter>();

            services.AddApplicationInsightsTelemetry(options);

            if(!string.IsNullOrEmpty(applicationInsightsSection.QuickPulseApiKey))
                services.ConfigureTelemetryModule<QuickPulseTelemetryModule>((module, o) => module.AuthenticationApiKey = applicationInsightsSection.QuickPulseApiKey);

            services.AddSingleton<ITelemetryInitializer, LightTelemetryInitializer>();
            
            if (applicationInsightsSection.EnableKubernetes)
            {
                services.AddApplicationInsightsKubernetesEnricher();
            }
        }

        /// <summary>
        /// Ativa o middleware customizado AppInsightsTelemetry para capturar todos os eventos das API.
        /// e registrar logg no AppInsights com os detalhes adequados
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseTelemetry(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TelemetryMiddleware>();
        }
    }

    /// <summary>
    /// This telemetry don't will use the TelemetryClient because we need centrilaze all messages to AppInsights
    /// </summary>
    public class TelemetryMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Middleware Telemetry
        /// Exception 500
        /// </summary>
        /// <param name="next"></param>
        public TelemetryMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Invokes the logic of the middleware.
        /// Intercepet the swagger call
        /// </summary>
        /// <param name="context">given request path</param>
        /// <returns>A Task that completes when the middleware has completed processing.</returns>
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception e)
            {
                ((LightTelemetry)WorkBench.Telemetry).TrackException(e);
                throw;
            }

        }
    }
}
