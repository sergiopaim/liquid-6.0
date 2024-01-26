using Liquid.Base;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Liquid.Domain
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static class HttpResponseMessageExtensions
    {
        /// <summary>
        /// Convert to LightDomain after response server
        /// </summary>
        /// <param name="response">Http response message</param>
        /// <returns>LightDomain</returns>
        public static async Task<DomainResponse> ConvertToDomainAsync(this HttpResponseMessage response)
        {
            var value = await (response?.Content?.ReadAsStringAsync());
            return (DomainResponse)Convert.ChangeType(new Liquid.Base.DomainResponse() { Payload = JsonDocument.Parse(value) }, typeof(DomainResponse));
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
