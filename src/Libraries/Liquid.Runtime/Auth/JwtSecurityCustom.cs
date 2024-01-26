using Liquid.Base;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace Liquid.Runtime
{

    /// <summary>
    /// JWT ClaimTypes mapped between AAD and custom identity provider
    /// </summary>
    public static class JwtClaimTypes
    {
        /// <summary>
        /// Id of the user in the JWT identity
        /// </summary>
        public static string UserId
        {
            get { return "sub"; }
        }
    }

    /// <summary>
    /// Helper class to work with custom JWT security options
    /// </summary>
    public static class JwtSecurityCustom
    {
        /// <summary>
        /// Auth configuration used to enforce authentication
        /// </summary>
        public static AuthConfiguration Config { get; set; }

        private static X509Certificate2 certificate;
        /// <summary>
        /// JWT signing certificate
        /// </summary>
        public static X509Certificate2 Certificate
        {
            get
            {
                if (certificate is null)
                {
                    certificate = new X509Certificate2(Convert.FromBase64String(Config.JWTCertificate));
                }
                return certificate;
            }
        }

        /// <summary>
        /// Gets the original JWT from a user identity
        /// </summary>
        /// <param name="identity">User identity</param>
        /// <returns>JWT token</returns>
        public static string GetJwtToken(ClaimsIdentity identity)
        {
            return identity?.Claims?.FirstOrDefault(c => c.Type == "token")?.Value;
        }

        /// <summary>
        /// Verify if token was received and return a claims or if token is empty, return a new mock claims
        /// </summary>
        /// <param name="jwt">Token as string</param>
        /// <returns>ClaimsPrincipal</returns>
        public static ClaimsPrincipal DecodeToken(string jwt)
        {
            ClaimsIdentity claims = null;

            if (ValidateTokenAsync(jwt))
            {
                try
                {
                    if (!string.IsNullOrEmpty(jwt))
                        claims = new ClaimsIdentity(new JwtSecurityToken(jwtEncodedString: jwt).Claims, "Custom");
                }
                catch (Exception e)
                {
                    throw new LightException("Error when trying to decode JWT into User.Claims", e);
                }

                if (claims is null)
                {
                    return null;
                }
                else
                {
                    claims.AddClaim(new Claim("token", jwt));

                    //Validation PASSED
                    return new(claims);
                }
            }
            else
                return null;
        }

        private static bool ValidateTokenAsync(string jwt)
        {
            bool forceValidate = false;

            if (string.IsNullOrEmpty(jwt) || (!WorkBench.IsProductionEnvironment && !forceValidate))
                return true;

            var validationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidAudience = Config.JWTSelfIssuedAudience,
                ValidIssuer = Config.JWTSelfIssuer,
                IssuerSigningKey = new X509SecurityKey(Certificate)
            };

            try
            {
                //Throws an Exception as the token is invalid (expired, invalid-formatted, etc.)
                new JwtSecurityTokenHandler().ValidateToken(jwt, validationParameters, out var validatedToken);
                return validatedToken is not null;
            }
            catch (SecurityTokenException)
            {
                return false;
            }
        }

    }
}
