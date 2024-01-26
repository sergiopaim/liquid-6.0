using Liquid.Domain;
using Liquid.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Liquid.Base
{
    /// <summary>
    /// Base type for returning business data and critic from domain classes
    /// </summary>
    public class DomainResponse
    {
        /// <summary>
        /// Response data serialized support (bytes, JSON, XML and etc.)
        /// </summary>
        public virtual JsonDocument Payload { get; set; }

        /// <summary>
        /// Business critics produced by domain business logic
        /// </summary>
        public List<Critic> Critics { get; set; }

        /// <summary>
        /// Identifies the current operation
        /// </summary>
        public string OperationId { get; set; }
		
        /// <summary>
        /// When true indicate that some critics have a not found message
        /// </summary>
        [JsonIgnore]
        public bool NotContent { get; set; }

        /// <summary>
        /// When true indicate that some critics have a conflict message
        /// </summary>
        [JsonIgnore]
        public bool ConflictMessage { get; set; }

        /// <summary>
        /// When true indicate that some critics have a bad request message
        /// </summary>
        [JsonIgnore]
        public bool BadRequestMessage { get; set; }

        /// <summary>
        /// When true indicate that have some generic return message
        /// </summary>
        [JsonIgnore]
        public bool GenericReturnMessage { get; set; }

        /// <summary>
        /// Response status code for generic return
        /// </summary>
        [JsonIgnore]
        public StatusCode StatusCode { get; set; } = StatusCode.OK;

        /// <summary>
        /// Creates a new domain response
        /// </summary>
        public DomainResponse() { }

        /// <summary>
        /// Creates a new domain response
        /// </summary>
        /// <param name="payload">The json document payload</param>
        /// <param name="operationId">The id of the operation</param>
        /// <param name="critics">The list of domain critics</param>
        public DomainResponse(JsonDocument payload, string operationId = null, List<Critic> critics = null)
        {
            if (payload is not null)
                Payload = payload;

            Critics = critics ?? new();
            OperationId = operationId;
        }

        /// <summary>
        /// Creates a new domain response
        /// </summary>
        /// <param name="payload">The json document payload</param>
        /// <param name="context">The domain operatio context</param>
        /// <param name="handler">The domain critic handler</param>
        public DomainResponse(JsonDocument payload, ILightContext context, ICriticHandler handler)
        {
            if (payload is not null)
                Payload = payload;

            Critics = handler.Critics?.Select(c => (Critic)c).ToList() ?? new();
            NotContent = handler.HasNoContentError;
            ConflictMessage = handler.HasConflictError;
            BadRequestMessage = handler.HasBadRequestError;
            GenericReturnMessage = handler.HasNotGenericReturn;

            StatusCode = handler.StatusCode;
            OperationId = context.OperationId;
        }
    }

    /// <summary>
    /// Base type for returning business data and critic from domain classes
    /// </summary>
    public class Response<T>
    {
        /// <summary>
        /// Response data serialized support (bytes, JSON, XML and etc.)
        /// </summary>
        public T Payload { get; set; }

        /// <summary>
        /// Business critics produced by domain business logic
        /// </summary>
        public List<Critic> Critics { get; set; }

        /// <summary>
        /// Identifies the current operation
        /// </summary>
        public string OperationId { get; set; }
    }

    /// <summary>
    /// Base type for returning business data and critic from domain classes
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Business critics produced by domain business logic
        /// </summary>
        public List<Critic> Critics { get; set; }

        /// <summary>
        /// Identifies the current operation
        /// </summary>
        public string OperationId { get; set; }
    }
}
