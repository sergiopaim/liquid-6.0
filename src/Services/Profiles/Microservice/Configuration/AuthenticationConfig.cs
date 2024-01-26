using FluentValidation;
using Liquid.Runtime;
using System.Collections.Generic;

namespace Microservice.Configuration
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class AuthenticationConfig : LightConfig<AuthenticationConfig>
    {
        public string JWTSelfIssuer { get; set; }
        public string JWTSelfIssuedAudience { get; set; }
        public string JWTP12CertificateLocationOld { get; set; }
        public string JWTCertificatePasswordOld { get; set; }
        public string JWTP12CertificateLocation { get; set; }
        public string JWTCertificatePassword { get; set; }
        public string AADTenantId { get; set; }
        public string AADServicePrincipalId { get; set; }
        public string AADServicePrincipalPassword { get; set; }
        public List<AADGroupAsRole> AADGroupsAsRoles { get; set; }
        public override void Validate()
        {
            RuleFor(v => v.JWTSelfIssuer).NotEmpty().WithError("JWTSelfIssuer must not be empty");
            RuleFor(v => v.JWTSelfIssuedAudience).NotEmpty().WithError("JWTSelfIssuedAudience must not be empty");
            RuleFor(v => v.JWTP12CertificateLocationOld).NotEmpty().WithError("JWTP12CertificateLocationOld must not be empty");
            RuleFor(v => v.JWTCertificatePasswordOld).NotEmpty().WithError("JWTCertificatePasswordOld must not be empty");
            RuleFor(v => v.JWTP12CertificateLocation).NotEmpty().WithError("JWTP12CertificateLocation must not be empty");
            RuleFor(v => v.JWTCertificatePassword).NotEmpty().WithError("JWTCertificatePassword must not be empty");
            RuleFor(v => v.AADTenantId).NotEmpty().WithError("AADTenantId must not be empty");
            RuleFor(v => v.AADServicePrincipalId).NotEmpty().WithError("AADServicePrincipalId must not be empty");
            RuleFor(v => v.AADServicePrincipalPassword).NotEmpty().WithError("AADServicePrincipalPassword must not be empty");
        }
    }
    public class AADGroupAsRole
    {
        public string RoleName { get; set; }
        public string GroupObjectId { get; set; }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
