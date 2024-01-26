using Liquid.Base.Test;
using Liquid.Domain.Test;
using Liquid.Runtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Liquid.Domain
{
    /// <summary>
    /// Authentication Extension for startup
    /// </summary>
    public static class AuthenticationExtension
    {
        private static AuthConfiguration config;

        /// <summary>
        /// Enables a mock middleware for a non Production environments
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseWorkbenchByPass(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<WorkbenchMiddleware>();
        }

        /// <summary>
        /// Add JWT support on authentication
        /// </summary>
        /// <param name="services"></param>
        public static void AddWorkbenchAuth(this IServiceCollection services)
        {
            config = LightConfigurator.LoadConfig<AuthConfiguration>("Auth");
            JwtSecurityCustom.Config = config;

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();            
        }
    }

    /// <summary>
    /// Mock Middleware
    /// </summary>
    public class WorkbenchMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="next"></param>
        public WorkbenchMiddleware(RequestDelegate next)
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
            if (string.IsNullOrEmpty(context.Request.ContentType))
                context.Request.ContentType = "application/json";

            string token = string.Empty;

            var path = context?.Request.Path.Value.Replace("//", "/");

            AdjustableClock.AdjustByRequest(context.Request);

            if (!AuthHandler.HandleHttpInvoke(ref context, ref token))
                return;

            //If no valid authorization came from header, tries from query
            if (string.IsNullOrEmpty(token))
                token = HubHandler.HandleHttpInvoke(ref context, pathToCheck: path);

            if (!ReseedHandler.HandleHttpInvoke(ref context, pathToCheck: path, context.Request.Query["output"] == "text"))
                return;

            if (!StubHandler.HandleHttpInvoke(ref context, pathToCheck: path))
                return;

            if (!MessageBusHandler.HandleHttpInvoke(ref context, pathToCheck: path))
                return;

            await _next.Invoke(context);
        }
    }
}
