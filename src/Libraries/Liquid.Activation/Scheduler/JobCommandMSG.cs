using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;
using System;

namespace Liquid.Activation
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class LightJobFrequency : LightEnum<LightJobFrequency>
    {
        public int Order => GetOrder(Code);

        public static readonly LightJobFrequency Monthly = new(nameof(Monthly));
        public static readonly LightJobFrequency MonthlyAt = new(nameof(MonthlyAt));
        public static readonly LightJobFrequency Weekly = new(nameof(Weekly));
        public static readonly LightJobFrequency WeeklyAt = new(nameof(WeeklyAt));
        public static readonly LightJobFrequency Daily = new(nameof(Daily));
        public static readonly LightJobFrequency DailyAt = new(nameof(DailyAt));
        public static readonly LightJobFrequency Hourly = new(nameof(Hourly));
        public static readonly LightJobFrequency HourlyAt = new(nameof(HourlyAt));
        public static readonly LightJobFrequency EveryThirtyMinutes = new(nameof(EveryThirtyMinutes));
        public static readonly LightJobFrequency EveryFifteenMinutes = new(nameof(EveryFifteenMinutes));
        public static readonly LightJobFrequency EveryTenMinutes = new(nameof(EveryTenMinutes));
        public static readonly LightJobFrequency EveryFiveMinutes = new(nameof(EveryFiveMinutes));
        public static readonly LightJobFrequency EveryMinute = new(nameof(EveryMinute));

        public LightJobFrequency(string code) : base(code) { }
    }

    public class LightJobStatus : LightEnum<LightJobStatus>
    {
        public int Order => GetOrder(Code);

        public static readonly LightJobStatus Running = new(nameof(Running));
        public static readonly LightJobStatus Aborted = new(nameof(Aborted));
        public static readonly LightJobStatus Deactivated = new(nameof(Deactivated));

        public LightJobStatus(string code) : base(code) { }
    }

    public class JobCommandCMD : LightEnum<JobCommandCMD>
    {
        public static readonly JobCommandCMD Register = new(nameof(Register));
        public static readonly JobCommandCMD Acknowledge = new(nameof(Acknowledge));
        public static readonly JobCommandCMD NotFound = new(nameof(NotFound));

        public JobCommandCMD(string code) : base(code) { }
    }

    public class JobCommandMSG : LightJobMessage<JobCommandMSG, JobCommandCMD>
    {
        public string Frequency { get; set; }
        public int PartitionCount { get; set; }

        public int? DayOfMonth { get; set; }
        public DayOfWeek? DayOfWeek { get; set; }
        public int? Hour { get; set; }
        public int? Minute { get; set; }

        public string Status { get; set; }

        public override void Validate()
        {
            RuleFor(i => i.Microservice).NotEmpty().WithError("microservice must not be empty");
            RuleFor(i => i.Job).NotEmpty().WithError("job must not be empty");

            RuleFor(i => i.Frequency).Must(f => f is null || LightJobFrequency.IsValid(f)).WithError("frequency is invalid");
            RuleFor(i => i.Status).Must(s => s is null || LightJobStatus.IsValid(s)).WithError("status is invalid");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
