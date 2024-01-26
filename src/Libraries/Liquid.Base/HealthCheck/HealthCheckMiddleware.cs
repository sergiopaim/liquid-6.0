using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Liquid.Base
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static class HealthCheckExtension
    {
        /// <summary>
        /// Enables a mock middleware for a non Production environments
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseHealthCheck(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HealthCheckMiddleware>();
        }
    }

    /// <summary>
    /// Mock Middleware
    /// </summary>
    public class HealthCheckMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="next"></param>
        public HealthCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Middleware invoke process
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            if (context is null)
                return;

            if (context.Request.Path.Value.ToLower().Contains("/health"))
            {
                LightHealthResult healthResult = new()
                {
                    Status = LightHealth.HealthCheckStatus.Healthy.ToString()
                };

                context.Response.StatusCode = 200; // Success

                //TODO Refactor so the cartridges return their own health state not of theirs dependencies

                //LightHealth.CheckHealth(healthResult);
                //if (healthResult.CartridgesStatus.Any(r => r.Status != LightHealth.HealthCheckStatus.Healthy.ToString()))
                //{
                //    healthResult.Status = LightHealth.HealthCheckStatus.Unhealthy.ToString();
                //    context.Response.StatusCode = 503; // ServiceUnavailable
                //}
               
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");

                string jsonString = healthResult.ToJsonString();
                await context.Response.WriteAsync(jsonString);
                return;
            }

            await _next.Invoke(context);
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
