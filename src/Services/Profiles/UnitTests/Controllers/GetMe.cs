using Liquid.Base;
using Liquid.Domain;
using Liquid.Domain.Test;
using System.Net;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class GetMe : LightUnitTestCase<GetMe, Fixture>
    {
        public GetMe(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("profGetMeMember")]
        [InlineData("profGetMeBOAdmin")]
        public void Success(string testId)
        {
            var testData = LoadTestData<DomainResponse>(testId);

            var input = testData.Input;
            var role = input.Property("role").AsString();

            var expectedId = testData.Output.Payload.Property("id").AsString();

            var response = Fixture.Api.WithRole(role)
                                       .Get<DomainResponse>("me");
            var domainResponse = response.Content;
            var resultId = domainResponse.Payload.Property("id").AsString();

            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.Equal(expectedId, resultId);
        }

        [Theory]
        [InlineData("UnknownUser")]
        public void UserNoContent(string role)
        {
            var response = Fixture.Api.WithRole(role).Get("me");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Theory]
        [InlineData("Guest")]
        public void Unauthorized(string role)
        {
            var response = Fixture.Api.WithRole(role).Get("me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

    }
}
