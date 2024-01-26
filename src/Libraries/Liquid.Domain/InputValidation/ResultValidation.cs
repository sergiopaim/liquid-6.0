using FluentValidation.Results;
using System.Collections.Generic;

namespace Liquid.Domain
{
    /// <summary>
    /// Class responsible to receive the result from the input validation on ViewModel
    /// </summary>
    public class ResultValidation
    {
        private readonly ValidationResult _validationResult;

        ///The method receives the propertie ValidationResult from the input validation from ViewModel
        ///and add on an errors list.
        public ResultValidation(ValidationResult validationResult)
        {
            Errors = new();
            _validationResult = validationResult;
            foreach (ValidationFailure failure in _validationResult?.Errors)
            {
                if (!string.IsNullOrEmpty(failure.ErrorCode))
                    Errors.TryAdd(failure.ErrorCode, new object[] { failure.ErrorMessage } );
                else
                    Errors.TryAdd(failure.ErrorMessage, new object[] { failure.ErrorMessage } );
            } 
        }

        /// <summary>
        /// Indication whether the result validation is valid
        /// </summary>
        public bool IsValid { get { return _validationResult.IsValid; } }

        /// <summary>
        /// List of result validadion errors
        /// </summary>
        public Dictionary<string, object[]> Errors { get; }
    }
}
