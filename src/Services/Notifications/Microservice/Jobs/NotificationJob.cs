using Liquid.Activation;
using Microservice.Services;
using System;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable IDE0060 // Remove unused parameter
namespace Microservice.Jobs
{
    [Scheduler("TRANSACTIONAL", "notifications")]
    public class NotificationJob : LightJobScheduler
    {
        [Job(nameof(LightJobFrequency.EveryTenMinutes))]
        public async void ReinforceByEmail(DateTime activation, int partition)
        {

            await Factory<NotificationService>().ReinforceByEmailAsync(activation.AddMinutes(-30), activation.AddMinutes(-20));

            Terminate();
        }
    }
}
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member