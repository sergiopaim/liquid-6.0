using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;
using Liquid.Repository;
using Microservice.ViewModels;
using System;
using System.Text.Json.Serialization;

namespace Microservice.Models
{
    public class Schedule : LightModel<Schedule>
    {
        public string Microservice { get; set; }    // the microservice (e.g. Tasks)
        public string Job { get; set; }   // the name of the job to be run (e.g. Notify user of upcoming checkin)
        public string Frequency { get; set; }
        public int PartitionCount { get; set; }

        public int? DayOfMonth { get; set; } = null;
        public DayOfWeek? DayOfWeek { get; set; } = null;
        public int? Hour { get; set; } = null;
        public int? Minute { get; set; } = null;

        public DateTime LastActivation { get; set; }
        public DateTime LastAcknowledged { get; set; }
        public DateTime NextActivation { get; set; }
        public int ActivationsSinceLastAcknowledge { get; set; } // more than 2 non-answered activations, inactivates the job and notifies DEV Team

        public string Status { get; set; }

        [JsonIgnore]
        public bool Flushed { get; set; } = true;

        public override void Validate()
        {
            RuleFor(i => Microservice).NotEmpty().WithError("microservice must not be empty");
            RuleFor(i => Job).NotEmpty().WithError("job must not be empty");
            RuleFor(i => Frequency).NotEmpty().WithError("frequency must not be empty");
            RuleFor(i => PartitionCount).NotEmpty().WithError("partitionCount must not be empty");
            RuleFor(i => NextActivation).NotEmpty().WithError("nextActivation must not be empty");
        }
    }
}

