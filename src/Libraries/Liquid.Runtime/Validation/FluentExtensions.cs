using FluentValidation;

namespace Liquid.Runtime
{    /// <summary>
     /// Implement Extensions of Fluent objects
     /// </summary>
    public static class FluentExtensions
    {
        /// <summary>
        /// Specifies a custom error code to use if validation fails.
        /// </summary>
        /// <param name="rule">The current rule</param>
        /// <param name="errorCode">The error code to use</param>
        /// <returns></returns>
        public static IRuleBuilderOptions<T, TProperty> WithError<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, string errorCode)
        {
            return rule.WithErrorCode(errorCode);
        }
    }
}