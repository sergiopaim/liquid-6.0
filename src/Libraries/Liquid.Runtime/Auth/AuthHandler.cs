using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace Liquid.Runtime
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class AuthHandler
    {
        public static bool HandleHttpInvoke(ref HttpContext context, ref string token)
        {
            bool tokenMustValidated = false;
            var authHeader = context.Request.Headers["Authorization"];
            if (authHeader != StringValues.Empty)
            {
                var header = authHeader.FirstOrDefault();
                if (!string.IsNullOrEmpty(header) && header.Contains("Bearer"))
                {
                    tokenMustValidated = true;
                }
                if (!string.IsNullOrEmpty(header) && header.StartsWith("Bearer ") && header.Length > "Bearer ".Length)
                {
                    token = header["Bearer ".Length..];
                    tokenMustValidated = true;
                }
            }

            try
            {
                if (tokenMustValidated)
                {
                    context.User = JwtSecurityCustom.DecodeToken(token);
                }
                return true;
            }
            catch (Exception e)
            {
                context.Response.StatusCode = 401;

                Console.WriteLine(e);

                _ = context.Response.WriteAsync("Authorization header is mal formed. Use Authorization = 'Bearer [JWT_CONTENT_AS_STRING]");
                return false;
            }
        }

        public static SecurityKey AddSecurityKey()
        {
            var rsaConfig = LightConfigurator.LoadConfig<SigningCredentialsConfig>("SigningCredentials");

            RSAParameters rsaParameters = new()
            {
                D = Convert.FromBase64String(rsaConfig.D),
                DP = Convert.FromBase64String(rsaConfig.DP),
                DQ = Convert.FromBase64String(rsaConfig.DQ),
                Exponent = Convert.FromBase64String(rsaConfig.Exponent),
                InverseQ = Convert.FromBase64String(rsaConfig.InverseQ),
                Modulus = Convert.FromBase64String(rsaConfig.Modulus),
                P = Convert.FromBase64String(rsaConfig.P),
                Q = Convert.FromBase64String(rsaConfig.Q)
            };
            SecurityKey key = new RsaSecurityKey(rsaParameters)
            {
                KeyId = rsaConfig.KeyId
            };

            return key;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
