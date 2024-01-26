using Liquid;
using Liquid.Activation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Microservice.Services
{
    public class SchedulerDaemon : LightBackgroundTask
    {
        public SchedulerDaemon(IServiceScopeFactory serviceScopeFactory) : base(serviceScopeFactory) { }

        // crontab expression -> every minute
        protected override string Schedule => "* * * * *";

        public override async Task ProcessInScope(IServiceProvider serviceProvider)
        {
            try
            {
                await Factory<SchedulerService>().DispatchJobs();
            }
            catch (Exception e)
            {
                Exception moreInfo = new($"Exception inside SchedulerDeamon: {e.Message} \n ***********************************************************************************\n", e);
                WorkBench.Telemetry.TrackException(moreInfo);

                WorkBench.ConsoleWriteLine(e.ToString());
            }
        }
    }
}
