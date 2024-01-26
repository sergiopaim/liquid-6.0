using Microsoft.AspNetCore.Builder;

namespace Liquid.Runtime
{

    /// <summary>
    /// Include support of Cors, that processing data included on Configuration file.
    /// </summary>
    public static class Cors
    {
        /// <summary>
        /// Use CrossOrigin for Liquid Microservice.
        /// </summary>
        /// <param name="builder"></param>
        public static IApplicationBuilder UseCrossOrigin(this IApplicationBuilder builder)
        {
            //Localhost in non-production environments othen then Development allows for connecting localhost (dev) frontends to local microservices running as INT, QA, etc.
            //One should configure in appsettings.*.json of Liquid.Runtime the localhost and ports for each app host in use
            //
            //NOTE: When hosted on Kubernetes in non-production environments, CORS management is responsability of ng-inx ingress (via configuration)
            //      When hosted on Kubernetes in production environments, CORS management is responsability of API Management PaaS (also via configuration)
            if (!WorkBench.IsProductionEnvironment)
            {
                var config = LightConfigurator.LoadConfig<LocalCorsConfiguration>("LocalCors");

                builder.UseCors(b =>
                {
                    b.WithOrigins(config.LocalOrigins.ToArray());
                    b.AllowAnyMethod();
                    b.AllowAnyHeader();
                    b.AllowCredentials();
                });
            }

            return builder;
        }
    }
}
