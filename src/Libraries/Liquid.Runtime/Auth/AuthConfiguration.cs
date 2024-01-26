namespace Liquid.Runtime
{

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class AuthConfiguration : LightConfig<AuthConfiguration>
    {
        public string JWTCertificate { get; set; }
        public string JWTSelfIssuer { get; set; }
        public string JWTSelfIssuedAudience { get; set; }
        public string SysAdminJWT { get; set; }
        
        public override void Validate() { }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}