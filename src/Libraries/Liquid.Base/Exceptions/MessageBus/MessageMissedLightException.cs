using System;
using System.Runtime.Serialization;

namespace Liquid.Base
{
    /// <summary>
    /// Class responsible for building the Business Exceptions
    /// </summary>
    [Serializable]
    public class MessageMissedLightException: LightException, ISerializable
    {
        /// <summary>
        /// Throws a message bus exception with contextual information
        /// </summary>
        public MessageMissedLightException(string source) : base(source) { }
    }
}
