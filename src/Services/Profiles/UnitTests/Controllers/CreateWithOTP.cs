using Liquid.Base;
using Liquid.Domain;
using Liquid.Platform;
using Liquid.Domain.Test;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class CreateWithOTP : LightUnitTestCase<CreateWithOTP, Fixture>
    {
        public CreateWithOTP(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("profCreateWithOTP_Success_New")]
        [InlineData("profCreateWithOTP_Success_Update")]
        public void Success(string testId)
        {
            var testData = LoadTestData<JsonDocument>(testId);

            var input = testData.Input;
            var expectedOutput = testData.Output.Property("payload").ToObject<ProfileWithOTPVM>();
            var expectedCommand = testData.Output.Property("command").ToObject<string>();

            var response = Fixture.Api.Post<DomainResponse>("", input);
            var domainResponse = response.Content;
            var result = domainResponse.Payload.ToObject<ProfileWithOTPVM>();

            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.Equal(expectedOutput?.Name, result?.Name);

            var msg = Fixture.MessageBus.InterceptedMessages.OfType<ProfileMSG>().FirstOrDefault();

            Assert.True((msg is null && expectedCommand is null) ||
                        (msg.CommandType == expectedCommand && msg.Id == expectedOutput?.Id));
        }
    }
}
