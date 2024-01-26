using FluentValidation;
using Liquid.Repository;
using Liquid.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microservice.Models
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class Config : LightModel<Config>
    {
        public string Name { get; set; }
        public string Language { get; set; }
        public string TimeZone { get; set; }
        public WebPushChannel WebPushChannel { get; set; } = new();
        public EmailChannel EmailChannel { get; set; } = new();
        public PhoneChannel PhoneChannel { get; set; } = new();

        public override void Validate()
        {
            RuleFor(i => i.Name).NotEmpty().WithError("name must not be empty");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}