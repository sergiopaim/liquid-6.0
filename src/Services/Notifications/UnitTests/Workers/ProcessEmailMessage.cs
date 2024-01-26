using Liquid.Base;
using Liquid.Domain;
using Liquid.Domain.Test;
using System.Net;
using Xunit;

namespace UnitTests.Workers
{
    [Collection("General")]
    public class ProcessEmailMessage : LightUnitTestCase<ProcessEmailMessage, Fixture>
    {
        public ProcessEmailMessage(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("notiEmailMessageSend")]
        public void Success(string testId)
        {
            var emailMSG = LoadTestData<DomainResponse>(testId).Input;

            var response = Fixture.MessageBus.SendToQueue("user/emails", emailMSG);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(CriticHandler.FromResponse(response.Content).HasBusinessErrors);
        }
    }
}
