using Liquid.Base;
using Liquid.Domain;
using Liquid.Platform;
using Liquid.Domain.Test;
using System.Collections.Generic;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class Notify : LightUnitTestCase<Notify, Fixture>
    {
        public Notify(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("notiNotificationSend")]
        public void NotSentAndForwarded(string testId)
        {
            var testData = LoadTestData<DomainResponse>(testId);
            var notiflVM = testData.Input;

            var response = Fixture.Api.Post<DomainResponse>("notify", notiflVM);
            var domainResponse = response.Content;

            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);

            // call to `Api.Put("messageBus/intercept/enable")` should be placed in the fixture constructor
            // call to `Api.Put("messageBus/intercept/disable")` should be placed in the fixture dispose method

            // this assumes all messages will be of type `ProfileMSG`
            // if not, a new call should be made for each message type to be checked

            var interceptPath = $"messageBus/intercept/messages/{domainResponse.OperationId}/{nameof(NotificationMSG)}";
            var messageResponse = Fixture.Api.Get<DomainResponse>(interceptPath);
            var messageDomainResponse = messageResponse.Content;

            var interceptedMessages = messageDomainResponse.Payload.ToObject<List<NotificationMSG>>();

            Assert.Contains(interceptedMessages, i => i.CommandType == NotificationCMD.Send.Code);
        }
        [Theory]
        [InlineData("notiNotificationRegister")]
        public void SentAndRegistered(string testId)
        {
            var testData = LoadTestData<DomainResponse>(testId);
            var notiflVM = testData.Input;

            var response = Fixture.Api.Post<DomainResponse>("notify", notiflVM);
            var domainResponse = response.Content;

            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);

            // call to `Api.Put("messageBus/intercept/enable")` should be placed in the fixture constructor
            // call to `Api.Put("messageBus/intercept/disable")` should be placed in the fixture dispose method

            // this assumes all messages will be of type `ProfileMSG`
            // if not, a new call should be made for each message type to be checked

            var interceptPath = $"messageBus/intercept/messages/{domainResponse.OperationId}/{nameof(NotificationMSG)}";
            var messageResponse = Fixture.Api.Get<DomainResponse>(interceptPath);
            var messageDomainResponse = messageResponse.Content;

            var interceptedMessages = messageDomainResponse.Payload.ToObject<List<NotificationMSG>>();

            Assert.Contains(interceptedMessages, i => i.CommandType == NotificationCMD.Register.Code);
        }
    }
}
