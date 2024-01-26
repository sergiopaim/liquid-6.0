using Liquid.Base;
using Liquid.Domain;
using Liquid.Domain.Test;
using Microservice.ViewModels;
using System.Net;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class WebPushSubscribeAsync : LightUnitTestCase<WebPushSubscribeAsync, Fixture>
    {
        public WebPushSubscribeAsync(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("notiPostWebSubscription01")]
        [InlineData("notiPostWebSubscription02")]
        public void Success(string testId)
        {
            var testData = LoadTestData<DomainResponse>(testId);

            var input = testData.Input;
            var userId = input.Property("userId").AsString();
            var payload = input.Property("payload").ToJsonDocument();

            var expectedOutput = testData.Output.Payload.ToObject<WebPushEndpointVM>();

            var response = Fixture.Api.WithRole(userId).Post<DomainResponse>($"mine/web/devices", payload);
            var domainResponse = response.Content;
            var result = domainResponse.Payload.ToObject<WebPushEndpointVM>();

            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.Equal(expectedOutput.DeviceId, result.DeviceId);
        }

        [Theory]
        [InlineData("notiPostWebSubscriptionChannelAlreadySubscribed")]
        public void ChannelAlreadySubscribed(string testId)
        {
            var testData = LoadTestData<DomainResponse>(testId);

            var input = testData.Input;
            var userId = input.Property("userId").AsString();
            var payload = input.Property("payload").ToJsonDocument();

            var response = Fixture.Api.WithRole(userId).Post($"mine/web/devices", payload);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}