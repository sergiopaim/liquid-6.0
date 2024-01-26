using Liquid.Base;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Liquid.Domain
{
    /// <summary>
    /// Class responsible for return the InvalidInputLightException object
    /// to build the LightException
    /// 
    /// Important: This attribute is NOT inherited from Exception, and MUST be specified 
    /// otherwise serialization will fail with a SerializationException stating that
    /// "Type X in Assembly Y is not marked as serializable."
    /// </summary>
    [Serializable]
    public class InvalidInputLightException : LightException
    {
        /// <summary>
        /// Input errors list
        /// </summary>
        public List<Critic> InputErrors { get; } = new();

        /// <summary>
        /// Build the object Critic and add to inputErrors list
        /// to send the object InvalidInputLightException to LightController
        /// </summary>
        /// <param name="inputErrors"></param>
        public InvalidInputLightException(Dictionary<string, object[]> inputErrors) : base()
        {
            if (inputErrors is null)
                return;

            InputErrors.Clear();
            foreach (var error in inputErrors)
            {
                Critic critic = new();
                critic.AddError(error.Key, CriticHandler.LocalizeMessage(error.Key, error.Value));
                InputErrors.Add(critic);
            }
        }

        /// <summary>
        /// Building a LightException with detailed data
        /// </summary>
        /// <param name="info">The SerializationInfo holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected InvalidInputLightException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
