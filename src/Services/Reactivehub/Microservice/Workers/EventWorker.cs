using Liquid.Activation;
using Liquid.Platform;
using Microservice.Services;

namespace Microservice.Workers
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
{
    [MessageBus("TRANSACTIONAL")]
    public class EventWorker : LightWorker
    {
        [Topic("events/domain", "events", maxConcurrentCalls: 1, deleteAfterRead: false)]
        public void ProcessDomainEvent(DomainEventMSG eventMSG)
        {
            ValidateInput(eventMSG);

            if (eventMSG.CommandType == DomainEventCMD.Notify.Code)
                _ = Factory<ReactiveHubService>().NotifyDomainEvent(eventMSG).Result;

            Terminate();
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
