using Liquid.Base;
using Liquid.Domain;
using Liquid.Domain.Test;
using System.Net;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class GetByEmail : LightUnitTestCase<GetByEmail, Fixture>
    {
        public GetByEmail(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("profGetByEmail01")]
        public void Success(string testId)
        {
            var testData = LoadTestData<DomainResponse>(testId);

            var input = testData.Input;
            var email = input.Property("email").AsString();

            var expectedId = testData.Output.Payload.Property("id").AsString();

            var response = Fixture.Api.Get<DomainResponse>($"byemail/{email}");
            var domainResponse = response.Content;
            var resultId = domainResponse.Payload.Property("id").AsString();

            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.Equal(expectedId, resultId);
        }

        [Theory]
        [InlineData("profGetByEmailNoContent")]
        public void UserNoContent(string testId)
        {
            var email = LoadTestData(testId).Input.Property("email").AsString();
            var response = Fixture.Api.Get($"byemail/{email}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Theory]
        [InlineData("Guest")]
        public void Unauthorized(string role)
        {
            var response = Fixture.Api.WithRole(role).Get("byemail/a");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
