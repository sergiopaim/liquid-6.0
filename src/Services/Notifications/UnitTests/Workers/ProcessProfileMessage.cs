using Liquid.Base;
using Liquid.Domain;
using Liquid.Domain.Test;
using System.Net;
using Xunit;

namespace UnitTests.Workers
{
    [Collection("General")]
    public class ProcessProfileMessage : LightUnitTestCase<ProcessProfileMessage, Fixture>
    {
        public ProcessProfileMessage(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("notiUserMessageCreate")]
        [InlineData("notiUserMessageUpdate")]
        [InlineData("notiUserMessageDelete")]
        public void Success(string testId)
        {
            var userMSG = LoadTestData<DomainResponse>(testId).Input;

            var response = Fixture.MessageBus.SendToTopic("user/profiles", userMSG);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(CriticHandler.FromResponse(response.Content).HasBusinessErrors);
        }
    }
}
