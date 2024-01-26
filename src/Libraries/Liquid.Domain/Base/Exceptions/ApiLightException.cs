using Liquid.Base;
using System;

namespace Liquid.Domain
{
    /// <summary>
    /// Class responsible for return the ApiLightExceptions object
    /// to build the LightException
    /// 
    /// Important: This attribute is NOT inherited from Exception, and MUST be specified 
    /// otherwise serialization will fail with a SerializationException stating that
    /// "Type X in Assembly Y is not marked as serializable."
    /// </summary>
    [Serializable]
    public class ApiLightException : LightException
    {
        /// <summary>
        /// Building an exception with a message 
        /// </summary>
        /// <param name="message">the message showed on the ModelView</param>
        public ApiLightException(string message) : base(message) {}

        /// <summary>
        /// Building an exception with a message 
        /// </summary>
        /// <param name="message">message showed on the ModelView</param>
        /// <param name="innerException">describes the error that caused the current exception</param>
        public ApiLightException(string message, Exception innerException) : base(message, innerException) {}
    }
}
