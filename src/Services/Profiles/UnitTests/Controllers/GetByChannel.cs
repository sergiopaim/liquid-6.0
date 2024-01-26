using Liquid.Base;
using Liquid.Domain;
using Liquid.Platform;
using Liquid.Domain.Test;
using System.Net;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class GetByChannel : LightUnitTestCase<GetByChannel, Fixture>
    {
        public GetByChannel(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("profGetIdByEmail01")]
        [InlineData("profGetIdByPhone01")]
        public void Success(string testId)
        {
            var testData = LoadTestData<DomainResponse>(testId);

            var input = testData.Input;
            var channel = input.Property("channel").AsString();

            var expectedId = testData.Output.Payload.Property("id").AsString();

            var response = Fixture.Api.Get<DomainResponse>($"byChannel/{channel}");
            var domainResponse = response.Content;
            var profile = domainResponse.Payload.ToObject<ProfileBasicVM>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.Equal(expectedId, profile.Id);
        }

        [Theory]
        [InlineData("profGetIdByEmailNoContent")]
        [InlineData("profGetIdByEmailAAD")]
        public void UserNoContent(string testId)
        {
            var channel = LoadTestData(testId).Input.Property("channel").AsString();
            var response = Fixture.Api.Get($"byChannel/{channel}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
