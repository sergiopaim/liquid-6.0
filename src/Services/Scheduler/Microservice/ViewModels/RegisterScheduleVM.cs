using FluentValidation;
using Liquid.Activation;
using Liquid.Domain;
using Liquid.Runtime;
using System;

namespace Microservice.ViewModels
{
    public class RegisterScheduleVM : LightViewModel<RegisterScheduleVM>
    {
        public string Microservice { get; set; }    // the microservice (e.g. Tasks)
        public string Job { get; set; }   // the job to be run (e.g. Notify user of upcoming checkin)
        public string Frequency { get; set; }
        public int PartitionCount { get; set; }  // the partition count as defined in the job declaration (is this relevant here?)
        public int? DayOfMonth { get; set; }
        public DayOfWeek? DayOfWeek { get; set; }
        public int? Hour { get; set; }
        public int? Minute { get; set; }
        public string Status { get; set; }

        public override void Validate()
        {
            RuleFor(i => Microservice).NotEmpty().WithError("microservice must not be empty");
            RuleFor(i => Job).NotEmpty().WithError("job must not be empty");
            RuleFor(i => Frequency).NotEmpty().WithError("frequency must not be empty");
            RuleFor(i => Frequency).Must(
                f => LightJobFrequency.IsValid(f)
            ).WithError("frequencyS is invalid");

            RuleFor(i => Frequency).Must(f => ValidateFrequency(f)).WithError("invalid frequency or time parameters");

            RuleFor(i => DayOfMonth).Must(
                d => !d.HasValue || d.HasValue && Math.Clamp(d.Value, 1, 31) == d  // null or in [1, 30]
            ).WithError("dayOfMonth is invalid");

            RuleFor(i => Hour).Must(
                h => !h.HasValue || h.HasValue && Math.Clamp(h.Value, 0, 23) == h  // null or in [0, 23]
            ).WithError("hour is invalid");

            RuleFor(i => Minute).Must(
                m => !m.HasValue || m.HasValue && Math.Clamp(m.Value, 0, 59) == m  // null or in [0, 59]
            ).WithError("Minute is invalid");

            RuleFor(i => PartitionCount).NotEmpty().WithError("partitionCount must not be empty");
        }

        private bool ValidateFrequency(string f)
        {
            if (f == LightJobFrequency.HourlyAt.Code && !Minute.HasValue)
                return false;
            if (f == LightJobFrequency.DailyAt.Code && !Hour.HasValue)
                return false;
            if (f == LightJobFrequency.WeeklyAt.Code && !DayOfWeek.HasValue)
                return false;
            if (f == LightJobFrequency.MonthlyAt.Code && !DayOfMonth.HasValue)
                return false;
            return true;
        }
    }
}