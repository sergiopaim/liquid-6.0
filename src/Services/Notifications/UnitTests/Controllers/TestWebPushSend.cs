using Liquid.Base;
using Liquid.Domain;
using Liquid.Domain.Test;
using System.Net;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class TestWebPushSend : LightUnitTestCase<TestWebPushSend, Fixture>
    {
        public TestWebPushSend(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("notiWebPushSend01")]
        public void Success(string testId)
        {
            var testData = LoadTestData<DomainResponse>(testId);

            var input = testData.Input;
            var payload = input.Property("payload").ToJsonDocument();

            var response = Fixture.Api.Post<DomainResponse>("test/webpush/send", payload);
            var domainResponse = response.Content;
            var result = domainResponse.Payload.ToObject<int>();

            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.Equal(0, result);
        }

        [Theory]
        [InlineData("notiWebPushSendUserNoContent")]
        public void UserNoContent(string testId)
        {
            var input = LoadTestData(testId).Input;
            var payload = input.Property("payload").ToJsonDocument();

            var response = Fixture.Api.Post("test/webpush/send", payload);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
