using Liquid;
using Liquid.Middleware;
using Liquid.OnAzure;
using Microservice.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microservice
{
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
            services.AddSingleton<IHostedService, SchedulerDaemon>();
        }

        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            WorkBench.UseTelemetry<AppInsights>();
            WorkBench.UseRepository<CosmosDB>();
            WorkBench.UseWorker<ServiceBusWorker>();

            app.UseWorkBench();

            app.UseMvc();
        }
    }
}
