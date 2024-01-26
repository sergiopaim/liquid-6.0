using System;
using System.Runtime.Serialization;

namespace Liquid.Base
{
    /// <summary>
    /// Class responsible for building the Business Exceptions
    /// </summary>
    [Serializable]
    public class DeadLetterLightException : LightException, ISerializable
    {
        /// <summary>
        /// Throws a deadletter exception with contextual information
        /// </summary>
        public DeadLetterLightException(string reason, string error) : base($"{reason}\n*********\n{error}") { }
    }
}
