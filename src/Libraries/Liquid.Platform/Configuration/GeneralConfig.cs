using FluentValidation;
using Liquid.Runtime;
using System;
using System.Collections.Generic;

namespace Liquid.Platform
{
    internal class GeneralConfig : LightConfig<GeneralConfig>
    {
        public Dictionary<string, Uri> AppURLs { get; set; }

        public override void Validate()
        {
            RuleFor(v => v.AppURLs).NotEmpty().WithError($"App URL list cannot be empty in the 'Notification' config entry in app.settings.{WorkBench.EnvironmentName} file.");
            RuleFor(v => v.AppURLs.Count).GreaterThan(0).WithError($"App URL list cannot be empty in the 'Notification' config entry in app.settings.{WorkBench.EnvironmentName} file.");         
        }
    }
}