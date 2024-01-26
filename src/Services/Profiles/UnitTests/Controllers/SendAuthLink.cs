using Liquid.Base;
using Liquid.Domain;
using Liquid.Domain.Test;
using Liquid.Platform;
using Microservice.Models;
using System.Linq;
using System.Net;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class SendAuthLink : LightUnitTestCase<SendAuthLink, Fixture>
    {
        public SendAuthLink(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("6c5f063d-22f7-4635-9849-02ea54d9b9cc", "email")]
        [InlineData("6c5f063d-22f7-4635-9849-02ea54d9b9cc", "phone")]
        public void Success(string accountId, string channelType)
        {
            var response = Fixture.Api
                                  .Anonymously()
                                  .Put<DomainResponse>($"auth/sendLink?accountId={accountId}&channelType={channelType}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var domainResponse = response.Content;
            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);

            if (channelType == ChannelType.Email.Code)
            {
                var email = Fixture.MessageBus.InterceptedMessages.OfType<EmailMSG>().FirstOrDefault();
                Assert.Equal(accountId, email.UserId);
            }
            else
            {
                var text = Fixture.MessageBus.InterceptedMessages.OfType<ShortTextMSG>().FirstOrDefault();
                Assert.Equal(accountId, text.UserId);
            }
        }
    }
}

