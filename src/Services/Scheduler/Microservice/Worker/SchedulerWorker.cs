using Liquid;
using Liquid.Activation;
using Liquid.Base;
using Microservice.Services;
using Microservice.ViewModels;

namespace Microservice.Workers
{
    [MessageBus("TRANSACTIONAL")]
    public class SchedulerWorker : LightWorker
    {
        [Queue("scheduler/commands", maxConcurrentCalls: 1, deleteAfterRead: false)]
        public async void ProcessJobCommand(JobCommandMSG jobCommandMSG)
        {
            ValidateInput(jobCommandMSG);

            if (jobCommandMSG.CommandType == JobCommandCMD.Register.Code)
            {
                var registerScheduleVM = RegisterScheduleVM.FactoryFrom(jobCommandMSG);
                await Factory<SchedulerService>().RegisterJob(registerScheduleVM);
            }
            else if (jobCommandMSG.CommandType == JobCommandCMD.Acknowledge.Code)
            {
                WorkBench.ConsoleWriteLine($"{WorkBench.UtcNow} Acknowledging {jobCommandMSG.Microservice}-{jobCommandMSG.Job}");

                var r = await Factory<SchedulerService>().AcknowledgeActivationAsync(jobCommandMSG.Microservice, jobCommandMSG.Job);

                if (r is null)
                    WorkBench.ConsoleWriteHighlightedLine($"{WorkBench.UtcNow} *** ATENTION *** \\nFailed to acknowledge {jobCommandMSG.Microservice}-{jobCommandMSG.Job}");
            }
            else if (jobCommandMSG.CommandType == JobCommandCMD.NotFound.Code)
            {
                WorkBench.ConsoleWriteLine($"{WorkBench.UtcNow} Removing not found Job {jobCommandMSG.Microservice}-{jobCommandMSG.Job}");

                var r = await Factory<SchedulerService>().RemoveJobAsync(jobCommandMSG.Microservice, jobCommandMSG.Job);

                if (r is null)
                    WorkBench.ConsoleWriteHighlightedLine($"{WorkBench.UtcNow} ** ATENTION *** \\nFailed to remove not found job {jobCommandMSG.Microservice}-{jobCommandMSG.Job}");
            }
            else
            {
                WorkBench.ConsoleWriteHighlightedLine($"{WorkBench.UtcNow} ** ATENTION *** \\nReceived an unknown message command:\\n {jobCommandMSG.ToJsonString(true)}");
            }

            Terminate();
        }
    }
}
