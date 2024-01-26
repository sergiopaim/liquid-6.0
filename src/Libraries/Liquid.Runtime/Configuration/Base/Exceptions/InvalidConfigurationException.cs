using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Liquid.Runtime
{ 
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    [Serializable]
    public class InvalidConfigurationException : Exception
    {
        public Dictionary<string, object[]> InputErrors { get; } = new();

        public InvalidConfigurationException(string message) : base(message) { }

        public InvalidConfigurationException(Dictionary<string, object[]> inputErrors) : base(string.Concat(inputErrors))
        {
            InputErrors = inputErrors;
        }

        protected InvalidConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
