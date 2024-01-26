using Liquid.Base;
using Liquid.Domain;
using Liquid.Domain.Test;
using System.Net;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class GetMyAccount : LightUnitTestCase<GetMyAccount, Fixture>
    {
        public GetMyAccount(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("profGetMyAccount01")]
        public void Success(string testId)
        {
            var testData = LoadTestData<DomainResponse>(testId);

            var input = testData.Input;
            var role = input.Property("role").AsString();

            var expectedId = testData.Output.Payload.Property("id").AsString();

            var response = Fixture.Api.WithRole(role)
                                           .Get<DomainResponse>("me/account");
            var domainResponse = response.Content;
            var resultId = domainResponse.Payload.Property("id").AsString();

            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.Equal(expectedId, resultId);
        }

        [Theory]
        [InlineData("Guest")]
        public void Unauthorized(string role)
        {
            var response = Fixture.Api.WithRole(role).Get("me/account");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

    }
}
