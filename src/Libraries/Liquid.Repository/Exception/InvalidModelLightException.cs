using Liquid.Base;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json;

namespace Liquid.Domain
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Class indicates the errors on the model layer
    /// </summary>
    [Serializable()]
    public class InvalidModelLightException : LightException
    {
        public List<Critic> InputErrors { get; } = new();

        public override string Message => "Invalid model. Check the structure of the submitted model:\n" +
                                           new { errors = InputErrors }.ToJsonString(true);

        public InvalidModelLightException(Dictionary<string, object[]> inputErrors) : base()
        {
            InputErrors.Clear();

            if (inputErrors is null)
                return;

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
        protected InvalidModelLightException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}