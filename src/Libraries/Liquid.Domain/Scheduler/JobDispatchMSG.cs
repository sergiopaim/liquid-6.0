using FluentValidation;
using Liquid.Runtime;
using System;

namespace Liquid.Domain
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class JobDispatchCMD : LightEnum<JobDispatchCMD>
    {
        public static readonly JobDispatchCMD Trigger = new(nameof(Trigger));
        public static readonly JobDispatchCMD Abort = new(nameof(Abort));

        public JobDispatchCMD(string code) : base(code) { }
    }

    public class JobDispatchMSG : LightJobMessage<JobDispatchMSG, JobDispatchCMD>
    {
        // The partition Id of the dispatch
        public int Partition { get; set; }
        // Datetime the dispatch was sheduled to activate
        public DateTime Activation { get; set; }

        public override void Validate()
        {
            RuleFor(i => i.Microservice).NotEmpty().WithError("microservice must not be empty");
            RuleFor(i => i.Job).NotEmpty().WithError("job must not be empty");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
