using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Liquid.Runtime
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static class WebApiVersion
    {
        private static SwaggerConfiguration config;

        /// <summary>
        /// Add API Versioning the default version is 1.0
        /// and we're going to read the version number from the media type
        /// incoming requests should have a accept header like this: Accept: application/json;v=1.0
        /// </summary>
        /// <param name="services"></param>
        public static void AddApiVersion(this IServiceCollection services)
        {
            config = LightConfigurator.LoadConfig<SwaggerConfiguration>("Swagger");
            var ver = config.Versions.FirstOrDefault(p => p.Name == config.ActiveVersion);

            if (ver is not null)
            {
                services.AddApiVersioning(o =>
                {
                    o.DefaultApiVersion = new ApiVersion(int.Parse(ver.Info.Version), 0);
                    o.AssumeDefaultVersionWhenUnspecified = true;
                    o.ApiVersionReader = new MediaTypeApiVersionReader();
                });
            }
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
