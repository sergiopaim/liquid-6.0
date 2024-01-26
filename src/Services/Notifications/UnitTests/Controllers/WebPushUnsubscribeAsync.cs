using Liquid.Base;
using Liquid.Domain;
using Liquid.Domain.Test;
using Microservice.ViewModels;
using System.Net;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class WebPushUnsubscribeAsync : LightUnitTestCase<WebPushUnsubscribeAsync, Fixture>
    {
        public WebPushUnsubscribeAsync(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("notiDeleteWebSubscription01")]
        [InlineData("notiDeleteWebSubscription02")]
        public void Success(string testId)
        {
            var testData = LoadTestData<DomainResponse>(testId);

            var input = testData.Input;
            var userId = input.Property("userId").AsString();
            var deviceId = input.Property("deviceId").AsString();

            var expectedOutput = testData.Output.Payload.ToObject<WebPushEndpointVM>();

            var response = Fixture.Api.WithRole(userId).Delete<DomainResponse>($"mine/web/devices/{deviceId}");
            var domainResponse = response.Content;
            var result = domainResponse.Payload.ToObject<WebPushEndpointVM>();

            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.Equal(expectedOutput.DeviceId, result.DeviceId);
        }

        [Theory]
        [InlineData("notiDeleteWebSubscriptionChannelNoContent")]
        public void ChannelNoContent(string testId)
        {
            var input = LoadTestData(testId).Input;
            var userId = input.Property("userId").AsString();
            var deviceId = input.Property("deviceId").AsString();

            var response = Fixture.Api.WithRole(userId).Delete($"mine/web/devices/{deviceId}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
