using Liquid.Base;
using Liquid.Domain;
using Liquid.Platform;
using Liquid.Domain.Test;
using System.Net;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class RevertChannelByIdAsync : LightUnitTestCase<RevertChannelByIdAsync, Fixture>
    {
        public RevertChannelByIdAsync(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("c4e221c1-f445-41bf-b624-29a518a41c94", "ewogICJpZCI6ICJjNGUyMjFjMS1mNDQ1LTQxYmYtYjYyNC0yOWE1MThhNDFjOTQiLAogICJvdHAiOiAiOTg4MjEiCn0=", "jsantos@members.com", "+55 (99) 99998-8956")]
        public void Success(string accountId, string otpToken, string oldEmail, string oldPhone)
        {
            var response = Fixture.Api
                                           .Anonymously()
                                           .Put<DomainResponse>($"{accountId}/channel/revert?otpToken={otpToken}");

            var domainResponse = response.Content;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var resultProfile = domainResponse.Payload.ToObject<ProfileVM>();

            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.Equal(oldEmail, resultProfile.Email);
            Assert.Equal(oldPhone, resultProfile.Phone);
        }
    }
}

