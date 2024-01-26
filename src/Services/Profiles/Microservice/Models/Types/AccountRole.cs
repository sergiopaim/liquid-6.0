using Liquid.Domain;

namespace Microservice.Models
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class AccountRole : LightEnum<AccountRole>
    {
        public static readonly AccountRole Member = new(nameof(Member));
        public static readonly AccountRole ServiceAccount = new(nameof(ServiceAccount));
        public AccountRole(string code) : base(code) { }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

}
