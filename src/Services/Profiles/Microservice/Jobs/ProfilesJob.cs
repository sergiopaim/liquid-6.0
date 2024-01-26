using Liquid.Activation;
using Microservice.Services;
using System;
using System.Threading.Tasks;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable IDE0060 // Remove unused parameter
namespace Microservice.Jobs
{
    [Scheduler("TRANSACTIONAL", "profiles")]
    public class ProfilesJob : LightJobScheduler
    {
        #region Schedule 24H
        [Job(nameof(LightJobFrequency.DailyAt), hour: 05, minute: 00)]
        public async Task SyncFromAADUsersAsync(DateTime activation, int partition)
        {
            await Factory<ProfileService>().SyncFromAADUsersAsync();

            Terminate();
        }

        #endregion
    }
}
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member