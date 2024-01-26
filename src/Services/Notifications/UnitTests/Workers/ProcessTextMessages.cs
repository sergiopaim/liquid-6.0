using Liquid.Base;
using Liquid.Domain;
using Liquid.Domain.Test;
using System.Net;
using Xunit;

namespace UnitTests.Workers
{
    [Collection("General")]
    public class ProcessTextMessages : LightUnitTestCase<ProcessTextMessages, Fixture>
    {
        public ProcessTextMessages(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("notiTextMessageSend")]
        public void Success(string testId)
        {
            var shortTextMSG = LoadTestData<DomainResponse>(testId).Input;

            var response = Fixture.MessageBus.SendToQueue("user/text", shortTextMSG);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(CriticHandler.FromResponse(response.Content).HasBusinessErrors);
        }
    }
}
