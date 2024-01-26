using FluentValidation;
using Liquid.Activation;
using Liquid.Domain;
using Liquid.Runtime;
using System;

namespace Microservice.ViewModels
{
    /// <summary>
    /// An report with all of its attributes
    /// </summary>
    public class ScheduleVM : LightViewModel<ScheduleVM>
    {
        public string Id { get; set; }
        public string Microservice { get; set; }    // the microservice (e.g. Tasks)
        public string Job { get; set; }   // the name of the job to be run (e.g. Notify user of upcoming checkin)
        public LightJobFrequency Frequency { get; set; }
        public int PartitionCount { get; set; }
        public int? DayOfMonth { get; set; }
        public DayOfWeek? DayOfWeek { get; set; }
        public int? Hour { get; set; }
        public int? Minute { get; set; }
        public DateTime LastActivation { get; set; }
        public DateTime LastAcknowledged { get; set; }
        public DateTime NextActivation { get; set; }
        public int ActivationsSinceLastAcknowledge { get; set; } // more than 2 non-answered activations, inactivates the job and notifies DEV Team
        public LightJobStatus Status { get; set; }

        /// <summary>
        /// Validates the ViewModel
        /// </summary>
        public override void Validate()
        {
            RuleFor(i => false).Equal(true).WithError("This ViewModel type can only be used as response");
        }
    }
}
