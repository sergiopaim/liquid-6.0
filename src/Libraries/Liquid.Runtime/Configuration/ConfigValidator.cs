using FluentValidation;

namespace Liquid.Runtime
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class ConfigValidator<T> : AbstractValidator<T>
    {
        public new ConfigResultValidation Validate(T input)
        {
            return new(base.Validate(input));
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
