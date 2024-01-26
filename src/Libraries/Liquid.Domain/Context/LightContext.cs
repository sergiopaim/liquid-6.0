using Liquid.Base;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Liquid.Domain
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Global Context for Microservice
    /// </summary>
    public class LightContext : ILightContext
    {
        /// <summary>
        /// User with Claims
        /// </summary>
        public ClaimsPrincipal User { get; set; }
        public string OperationId { get; set; }
        public IHttpContextAccessor HttpContextAccessor { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public LightContext() { }
        /// <summary>
        /// Constructor with HttpContext Accessor
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        public LightContext(IHttpContextAccessor httpContextAccessor)
        {
            HttpContextAccessor = httpContextAccessor;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
