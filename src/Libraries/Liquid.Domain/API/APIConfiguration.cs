using FluentValidation;

namespace Liquid.Runtime
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Validates the host property from APIWrapper
    /// </summary>
    public class ApiConfiguration : LightConfig<ApiConfiguration>
    {
        public string Host { get; set; }
        public int? Port { get; set; }
        public string Suffix { get; set; }
        public bool Stub { get; set; }

        public override void Validate()
        { 
            RuleFor(x => x.Host).NotEmpty().WithError("The Host property should be informed on API settings");
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}