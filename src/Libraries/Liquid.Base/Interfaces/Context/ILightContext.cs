using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Liquid.Base
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Global Context interface for Microservice
    /// </summary>
    public interface ILightContext
    {
        ClaimsPrincipal User { get; set; }
        string OperationId { get; set; }
        public IHttpContextAccessor HttpContextAccessor { get; }

    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
