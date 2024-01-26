using Liquid.Base;
using Liquid.Domain.API;
using System;

namespace Liquid.Domain.Test
{
    /// <summary>
    /// Helper class to test Scheduler jobs
    /// </summary>
    public class SchedulerTester
    {
        private readonly MessageBusTester bus;

        /// <summary>
        /// Instanciates a Scheduler tester 
        /// </summary>
        /// <param name="bus">the messsage bus tester</param>
        public SchedulerTester(MessageBusTester bus)
        {
            this.bus = bus;
        }

        /// <summary>
        /// Dispactches a job
        /// </summary>
        /// <returns>A domain response</returns>
        public HttpResponseMessageWrapper<DomainResponse> Dispactch(string job, DateTime activation, int partition = 1)
        {
            JobDispatchMSG msg = new()
            {
                Activation = activation,
                Partition = partition,
                Job = job,
                CommandType = JobDispatchCMD.Trigger.Code
            };

            return bus.SendToTopic($"messageBus/send/topic/{SchedulerMessageBus<MessageBrokerWrapper>.JOBS_ENDPOINT}", msg);
        }
    }
}