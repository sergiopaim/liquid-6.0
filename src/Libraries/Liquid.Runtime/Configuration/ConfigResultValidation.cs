using System.Collections.Generic;
using FluentValidation.Results;

namespace Liquid.Runtime
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class ConfigResultValidation
    {
        private readonly ValidationResult _validationResult;

        public ConfigResultValidation(ValidationResult validationResult)
        {
            Errors = new();
            _validationResult = validationResult;
            foreach (ValidationFailure failure in _validationResult?.Errors)
            {
                Errors.TryAdd(failure.ErrorMessage, new object[] { failure.ErrorMessage });
            }
        }

        public bool IsValid { get { return _validationResult.IsValid; } }

        public Dictionary<string, object[]> Errors { get; }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
