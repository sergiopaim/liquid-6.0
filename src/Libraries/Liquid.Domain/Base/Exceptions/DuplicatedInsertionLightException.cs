using Liquid.Base;
using System;
using System.Runtime.Serialization;

namespace Liquid.Domain
{
    /// <summary>
    /// Exception for duplicated insertion conflict detection
    /// </summary>
    [Serializable]
    public class DuplicatedInsertionLightException : LightException
    {
        /// <summary>
        /// Building a LightException with summary data
        /// </summary>
        /// <param name="modelName">The name of the model entity</param>
        public DuplicatedInsertionLightException(string modelName) : base($"An insertion conflict happend in repository for a '{modelName}' record.") { }

        /// <summary>
        /// Building a LightException with detailed data
        /// </summary>
        /// <param name="info">The SerializationInfo holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected DuplicatedInsertionLightException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}