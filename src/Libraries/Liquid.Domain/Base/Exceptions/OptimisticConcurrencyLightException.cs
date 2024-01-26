using Liquid.Base;
using System;
using System.Runtime.Serialization;

namespace Liquid.Domain
{
    /// <summary>
    /// Exception for optimistic concurrency conflict detection
    /// </summary>
    [Serializable]
    public class OptimisticConcurrencyLightException : LightException
    {
        /// <summary>
        /// Building a LightException with summary data
        /// </summary>
        /// <param name="modelName">The name of the model entity</param>
        public OptimisticConcurrencyLightException(string modelName) :
            base($"An optimistic concurrence conflict happend in repository for a 'LightOptimisticModel<{modelName}>' record.")
        {}

        /// <summary>
        /// Building a LightException with detailed data
        /// </summary>
        /// <param name="info">The SerializationInfo holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected OptimisticConcurrencyLightException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }
}