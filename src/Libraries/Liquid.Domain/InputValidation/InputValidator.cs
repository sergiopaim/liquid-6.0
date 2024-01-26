using FluentValidation;

namespace Liquid.Domain
{
    /// <summary>
    /// Validator of input
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class InputValidator<T> : AbstractValidator<T>
    {
        /// <summary>
        /// Validates the input
        /// </summary>
        /// <param name="input">The input to validate</param>
        /// <returns></returns>
        public new ResultValidation Validate(T input)
        {
            return new(base.Validate(input));
        }
    }
}
