using Liquid.Base;
using Liquid.Domain;
using Liquid.Platform;
using Liquid.Domain.Test;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class PutMeAsync : LightUnitTestCase<PutMeAsync, Fixture>
    {
        public PutMeAsync(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("profPutMeMember")]
        [InlineData("profPutMeBOAdmin")]
        public void Success(string testId)
        {
            var testData = LoadTestData<DomainResponse>(testId);

            var input = testData.Input;
            var role = input.Property("role").AsString();
            var payload = input.Property("payload").ToJsonDocument();

            var output = testData.Output.Payload;

            var expectedId = output.Property("id").AsString();
            var expectedTimeZone = output.Property("timeZone").AsString();

            var response = Fixture.Api.WithRole(role)
                                       .Put<DomainResponse>("me", payload);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var domainResponse = response.Content;
            var resultId = domainResponse.Payload.Property("id").AsString();
            var resultTimeZone = domainResponse.Payload.Property("timeZone").AsString();

            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.Equal(expectedId, resultId);
            Assert.Equal(expectedTimeZone, resultTimeZone);

        }

        [Theory]
        [InlineData("EmailConflict")]
        [InlineData("PhoneConflict")]
        public void Conflict(string testId)
        {
            var testData = LoadTestData<DomainResponse>(testId);

            var input = testData.Input;
            var role = input.Property("role").AsString();
            var payload = input.Property("payload").ToJsonDocument();

            var response = Fixture.Api.WithRole(role)
                                       .Put<DomainResponse>("me", payload);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var domainResponse = response.Content;

            Assert.True(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.Contains(CriticHandler.FromResponse(domainResponse).Critics, c => c.Code == "PHONE_OR_EMAIL_ALREADY_REGISTERED");
        }

        [Theory]
        [InlineData("EmailToBeValidated")]
        public void EmailChannelValidation(string testId)
        {
            var testData = LoadTestData<DomainResponse>(testId);

            var input = testData.Input;
            var role = input.Property("role").AsString();
            var payload = input.Property("payload").ToJsonDocument();

            var response = Fixture.Api.WithRole(role)
                                       .Put<DomainResponse>("me", payload);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var domainResponse = response.Content;

            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);

            var emails = Fixture.MessageBus.InterceptedMessages.OfType<EmailMSG>();

            var validationMsg = emails?[1];
            var revertMsg = emails?[0];

            var output = testData.Output.Payload;
            var expectedId = output.Property("id").AsString();
            var expectedEmail = output.Property("email").AsString();

            //Checks revert email
            Assert.Null(revertMsg?.Email);
            Assert.Contains(expectedEmail, revertMsg?.Message);

            //Checks validation email
            Assert.Equal(expectedId, validationMsg?.UserId);
            Assert.Equal(expectedEmail, validationMsg?.Email);
        }

        [Theory]
        [InlineData("PhoneToBeValidated")]
        public void PhoneChannelValidation(string testId)
        {
            var testData = LoadTestData<DomainResponse>(testId);

            var input = testData.Input;
            var role = input.Property("role").AsString();
            var payload = input.Property("payload").ToJsonDocument();

            var response = Fixture.Api.WithRole(role)
                                       .Put<DomainResponse>("me", payload);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var domainResponse = response.Content;

            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);

            var textMsg = Fixture.MessageBus.InterceptedMessages.OfType<ShortTextMSG>().FirstOrDefault();

            var output = testData.Output.Payload;
            var expectedId = output.Property("id").AsString();
            var expectedPhone = output.Property("phone").AsString();

            //Checks validation text message
            Assert.Equal(expectedId, textMsg?.UserId);
            Assert.Equal(expectedPhone, textMsg?.Phone);

            var revertMsg = Fixture.MessageBus.InterceptedMessages.OfType<EmailMSG>().FirstOrDefault();

            //Checks revert email
            Assert.Null(revertMsg?.Email);
            Assert.Contains(expectedPhone, revertMsg?.Message);
        }

        [Theory]
        [InlineData("EmailAndPhoneToBeValidated")]
        public void BothChannelsValidation(string testId)
        {
            var testData = LoadTestData<DomainResponse>(testId);

            var input = testData.Input;
            var role = input.Property("role").AsString();
            var payload = input.Property("payload").ToJsonDocument();

            var response = Fixture.Api.WithRole(role)
                                      .Put<DomainResponse>("me", payload);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var domainResponse = response.Content;

            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);

            var textMsg = Fixture.MessageBus.InterceptedMessages.OfType<ShortTextMSG>().FirstOrDefault();

            var output = testData.Output.Payload;
            var expectedId = output.Property("id").AsString();
            var expectedEmail = output.Property("email").AsString();
            var expectedPhone = output.Property("phone").AsString();

            //Checks validation text message
            Assert.Equal(expectedId, textMsg?.UserId);
            Assert.Equal(expectedPhone, textMsg?.Phone);

            var emails = Fixture.MessageBus.InterceptedMessages.OfType<EmailMSG>();

            var validationMsg = emails?[1];
            var revertMsg = emails?[0];

            //Checks revert email
            Assert.Null(revertMsg?.Email);
            Assert.Contains(expectedEmail, revertMsg?.Message);

            //Checks validation email
            Assert.Equal(expectedId, validationMsg?.UserId);
            Assert.Equal(expectedEmail, validationMsg?.Email);
        }

        [Theory]
        [InlineData("profPutMeNoContent")]
        public void UserNoContent(string testId)
        {
            var input = LoadTestData<DomainResponse>(testId).Input;
            var role = input.Property("role").AsString();
            var payload = input.Property("payload").ToJsonDocument();

            var response = Fixture.Api.WithRole(role).Put("me", payload);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Theory]
        [InlineData("Guest")]
        public void Unauthorized(string role)
        {
            var response = Fixture.Api.WithRole(role).Put("me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
