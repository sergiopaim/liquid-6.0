using Liquid.Base;
using System;
using System.Runtime.Serialization;

namespace Liquid.Repository
{ 
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    [Serializable]
    public class BadRepositoryInitializationLightException : LightException
    {
        public BadRepositoryInitializationLightException(string lightRepositoryTypeName) :
            base($"{lightRepositoryTypeName} repository not was correctly initialized. For direct instantiation, it must be constructed as the following example: {lightRepositoryTypeName} myNewRepo = new {lightRepositoryTypeName}(\"MYNEWREPO\")")
        { }

        /// <summary>
        /// Building a LightException with detailed data
        /// </summary>
        /// <param name="info">The SerializationInfo holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected BadRepositoryInitializationLightException(SerializationInfo info, StreamingContext context) : base(info, context) { }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}