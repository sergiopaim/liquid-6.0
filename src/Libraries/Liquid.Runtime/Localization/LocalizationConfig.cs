using FluentValidation;

namespace Liquid.Runtime
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class LocalizationConfig : LightConfig<LocalizationConfig>
    {
        public string DefaultCulture { get; set; }

        public string[] SupportedCultures { get; set; }

        public override void Validate()
        {
            RuleFor(x => DefaultCulture).NotEmpty().WithError("A Default Culture should be defined.");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
