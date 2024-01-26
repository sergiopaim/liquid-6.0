using Liquid.Base;
using Liquid.Domain;
using Liquid.Domain.Test;
using System.Net;
using Xunit;

namespace UnitTests.Workers
{
    [Collection("General")]
    public class ProcessNotificationMessage : LightUnitTestCase<ProcessNotificationMessage, Fixture>
    {
        public ProcessNotificationMessage(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("notiNotificationSend")]
        [InlineData("notiNotificationRegister")]
        public void Success(string testId)
        {
            var contextualMSG = LoadTestData<DomainResponse>(testId).Input;

            var response = Fixture.MessageBus.SendToQueue("user/notifs", contextualMSG);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(CriticHandler.FromResponse(response.Content).HasBusinessErrors);
        }
    }
}
