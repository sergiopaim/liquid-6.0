using FluentValidation;
using Liquid.Domain;
using Liquid.Runtime;

namespace Liquid.Repository
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class Validator<T> : AbstractValidator<T>
    {
        public new ResultValidation Validate(T input)
        {
            return new(base.Validate(input));
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
