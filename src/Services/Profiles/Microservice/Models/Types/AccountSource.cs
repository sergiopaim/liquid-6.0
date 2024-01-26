using Liquid.Domain;

namespace Microservice.Models
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class AccountSource : LightLocalizedEnum<AccountSource>
    {
        public static readonly AccountSource AAD = new(nameof(AAD));
        public static readonly AccountSource IM = new(nameof(IM));

        public AccountSource(string code) : base(code) { }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

}
