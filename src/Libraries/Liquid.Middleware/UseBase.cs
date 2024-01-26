using Liquid.Activation;
using Liquid.Base;
using Liquid.Domain;
using Liquid.Runtime;
using Liquid.Runtime.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Liquid.Middleware
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static class UseBase
    {
        private static bool usingAzureSignalR = true;

        public static IServiceCollection AddReactiveHub(this IServiceCollection service, string connectionString = null)
        {
            var s = service.AddSignalR();

            if (string.IsNullOrEmpty(connectionString))
            {
                if (WorkBench.IsDevelopmentEnvironment)
                {
                    usingAzureSignalR = false;
                }
                else
                {
                    throw new LightException("Missing Azure SignalR Service connection string.");
                }
            }
            else
            {
                s.AddAzureSignalR(connectionString);
            }

            return service;
        }

        public static IApplicationBuilder UseReactiveHub<T>(this IApplicationBuilder builder) where T : LightReactiveHub
        {

            if (!WorkBench.ServiceIsRegistered(WorkBenchServiceType.ReactiveHub))
                throw new LightException($"`{nameof(LightReactiveHub)}` service not registered.");

            var reactiveHub = (LightReactiveHub)WorkBench.GetRegisteredService(WorkBenchServiceType.ReactiveHub);
            var hubEndpoint = reactiveHub.GetHubEndpoint();
            if (string.IsNullOrEmpty(hubEndpoint))
                throw new LightException($"`{reactiveHub.GetType().Name}` class should be annotated with `{nameof(ReactiveHubAttribute)}`");

            if (usingAzureSignalR)
            {
                builder.UseAzureSignalR(routes =>
                {
                    routes.MapHub<T>(hubEndpoint);
                });
            }

            builder.UseRouting();
            builder.UseWorkbenchByPass();
            builder.UseAuthorization();

            if (!usingAzureSignalR)
            {
                builder.UseEndpoints(endpoints =>
                {
                    endpoints.MapHub<T>(hubEndpoint);
                });
            }
            return builder;
        }

        public static IServiceCollection AddWorkBench(this IServiceCollection service, IConfiguration Configuration)
        {
            WorkBench.Configuration = Configuration;
            //Add Secrets options

            service.AddHttpContextAccessor();

            //Enables MVC without Endpoint Routing as .NET 3.0 started to demand
            service.AddMvc(options => {
                                          options.EnableEndpointRouting = false;
                                          options.InputFormatters.Insert(0, new TextPlainInputFormatter());
                                      })
                   .AddJsonOptions(options => {
                                                  options.JsonSerializerOptions.PropertyNamingPolicy = LightGeneralSerialization.Default.PropertyNamingPolicy;
                                              
                                                  foreach (var converter in LightGeneralSerialization.Default.Converters)
                                                      options.JsonSerializerOptions.Converters.Add(converter);                  
                                              }); 

            //Inject Swagger (Open API specification) and API Versioning
            service.AddApiVersion();
            service.AddSwagger();

            //Inject JWT pattern and security
            service.AddWorkbenchAuth();

            service.AddTelemetry();

            return service;
        }

        /// <summary>
        /// Activates the WorkBench and the middleware of its services previously initiatedB.
        /// </summary>
        /// <param name="builder">The builder of the core application</param>
        /// <returns>The builder of the core application</returns>
        public static IApplicationBuilder UseWorkBench(this IApplicationBuilder builder)
        {
            //If a ILightTelemetry type was injected then injects its related middleware
            if (WorkBench.ServiceIsRegistered(WorkBenchServiceType.Telemetry))
            {
                builder.UseTelemetry();
            }

            // Inject Swagger for all microservices
            builder.UseOpenApiSwagger();
            builder.UseOpenApiConventions();

            //Inject ByPass Auth and others 
            builder.UseWorkbenchByPass();
            builder.UseAuthentication();

            builder.UseCrossOrigin();

            // Enables Json localization
            builder.AddLocalization();

            // Adicionando o serviço de Health Check
            builder.UseHealthCheck();

            Console.WriteLine($"Microservice started at {WorkBench.UtcNow}.");

            // Always injects the LightException handling middleware
            return builder.UseMiddleware<GeneralExceptionCatcherMiddlware>();
        }

        public class TextPlainInputFormatter : TextInputFormatter
        {
            public TextPlainInputFormatter()
            {
                SupportedMediaTypes.Add("text/plain");
                SupportedEncodings.Add(Encoding.UTF8);
                SupportedEncodings.Add(Encoding.Unicode);
            }

            protected override bool CanReadType(Type type)
            {
                // This formatter only supports string types.
                return type == typeof(string);
            }

            public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
            {
                // Read the request body.
                var request = context.HttpContext.Request;
                using var reader = new StreamReader(request.Body, encoding);
                var text = await reader.ReadToEndAsync();

                // Return the result.
                return await InputFormatterResult.SuccessAsync(text);
            }
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
