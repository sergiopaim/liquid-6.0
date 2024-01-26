using FluentValidation;
using System.Collections.Generic;

namespace Liquid.Runtime
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    ///  Configuration of CORS settings for localhost (DEV) environment.
    /// </summary>
    public class LocalCorsConfiguration : LightConfig<LocalCorsConfiguration>
    {
        public List<string> LocalOrigins { get; set; }

        public override void Validate()
        {
            if (WorkBench.IsDevelopmentEnvironment)
            {
                RuleFor(d => LocalOrigins).NotEmpty().WithError($"LocalOrigins (Local App URL list) cannot be empty in the 'LocalCors' config entry in app.settings.{WorkBench.EnvironmentName} file of LiquidApplication.Runtime library.");
                RuleFor(d => d.LocalOrigins.Count).GreaterThan(0).WithError($"LocalOrigins (Local App URL list) cannot be empty in the 'LocalCors' config entry in app.settings.{WorkBench.EnvironmentName} file of LiquidApplication.Runtime library.");
            }
        }
    }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
