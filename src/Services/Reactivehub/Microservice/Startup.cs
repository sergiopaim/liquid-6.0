using Liquid;
using Liquid.Middleware;
using Liquid.OnAzure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microservice.ReactiveHub;

namespace Microservice
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class Startup
    {
        //forced-deploy@v1.79.01

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkBench(Configuration);
            services.AddReactiveHub(SignalRConfiguration.GetConnectionString());
        }

        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            WorkBench.UseTelemetry<AppInsights>();
            WorkBench.UseRepository<CosmosDB>();
            WorkBench.UseReactiveHub<GeneralHub>();
            WorkBench.UseWorker<ServiceBusWorker>();

            app.UseWorkBench();

            app.UseReactiveHub<GeneralHub>();
            app.UseMvc();
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
