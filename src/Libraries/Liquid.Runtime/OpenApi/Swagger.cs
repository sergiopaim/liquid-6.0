using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;

namespace Liquid.Runtime
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Include support of Open API based on swagger, that processing data included on Configuration file.
    /// </summary>
    public static class Swagger
    {
        ///Model of Swagger Configuration details
        private static SwaggerConfiguration config;
        public static SwaggerConfiguration Config
        {
            get { return config; }
        }

        /// <summary>
        /// Add Swagger support for Microservice.
        /// </summary>
        /// <param name="services"></param>
        public static void AddSwagger(this IServiceCollection services)
        {
            config = LightConfigurator.LoadConfig<SwaggerConfiguration>("Swagger");

            // Fill all versions declareted
            services.AddSwaggerGen(c => ApplyOptions(c));
        }

        private static void ApplyOptions(SwaggerGenOptions c)
        {
            foreach (var version in config.Versions)
            {
                c.SwaggerDoc(version.Name, new OpenApiInfo
                {
                    Version = version.Name,
                    Title = version.Info.Title,
                    Description = version.Info.Description,
                    TermsOfService = new Uri("http://dev-team.your-domain.com"),
                    Contact = new OpenApiContact { Name = "Your DEV Team", Email = "dev-team@your-domain.com", Url = new Uri("http://dev-team.your-domain.com") },
                    License = new OpenApiLicense { Name = "Your API Licence", Url = new Uri("http://dev-team.your-domain.com") }
                });
            }
            c.IgnoreObsoleteActions();

            // Set the comments path for the swagger json and ui.
            var basePath = PlatformServices.Default.Application.ApplicationBasePath;
            var xmlPath = Path.Combine(basePath, $"swagger-comments.xml");

            if (File.Exists(xmlPath))
                c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

            c.SchemaFilter<SwaggerIgnoreFilter>();

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
            {
                Description = "Jwt authorization header using the bearer scheme. example: \"authorization: bearer {token}\"",
                Name = "Authorization",
                Scheme = "Bearer",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement() {
                    {
                    new OpenApiSecurityScheme {
                        Reference = new OpenApiReference {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = ParameterLocation.Header,
                    },
                    new List<string>()
                    }
                });

            c.EnableAnnotations();
        }

        /// <summary>
        /// Use Swagger for Liquid Microservice.
        /// </summary>
        /// <param name="builder"></param>
        public static IApplicationBuilder UseOpenApiSwagger(this IApplicationBuilder builder)
        {
            config = LightConfigurator.LoadConfig<SwaggerConfiguration>("Swagger");

            builder.UseSwaggerUI(c =>
            {
                // Fill all versions declared
                foreach (var version in config.Versions)
                {
                    c.SwaggerEndpoint($"{version.Name}/swagger.json", $"{version.Name} Docs");
                }
            });

            return builder;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
