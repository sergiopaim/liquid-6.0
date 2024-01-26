using System;
using System.Runtime.Serialization;

namespace Liquid.Base
{
    /// <summary>
    /// Class responsible for building the Business Exceptions
    /// </summary>
    [Serializable]
    public class BusinessLightException: LightException, ISerializable
    {
        /// <summary>
        /// Throws a business exception with contextual information
        /// </summary>
        /// <param name="businessCode">The code to identify the point of business failure</param>
        public BusinessLightException(string businessCode) : base(businessCode.Replace(" ", "_").ToUpper()) { }
    }
}
