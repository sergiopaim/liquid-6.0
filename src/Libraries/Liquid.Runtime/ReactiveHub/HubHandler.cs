using Liquid.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Liquid.Runtime
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class HubHandler
    {
        public static string HandleHttpInvoke(ref HttpContext context, string pathToCheck)
        {
            string token = null;

            var reactiveHub = (ILightReactiveHub)WorkBench.GetRegisteredService(WorkBenchServiceType.ReactiveHub);

            if (reactiveHub is not null && pathToCheck.StartsWith(reactiveHub.GetHubEndpoint()))
            {
                token = context.Request.Query["token"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.User = JwtSecurityCustom.DecodeToken(token);
                }
            }

            return token;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
