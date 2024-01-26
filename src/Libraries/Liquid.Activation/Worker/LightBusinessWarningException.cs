using Liquid.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Liquid.Activation
{
    [Serializable]
    internal class LightBusinessWarningException : Exception
    {

        public LightBusinessWarningException()
        {
        }

        public LightBusinessWarningException(string message) : base(message)
        {
        }

        public LightBusinessWarningException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected LightBusinessWarningException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}