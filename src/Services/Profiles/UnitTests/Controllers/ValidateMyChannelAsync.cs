using Liquid.Base;
using Liquid.Domain;
using Liquid.Platform;
using Liquid.Domain.Test;
using Microservice.Models;
using System.Net;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class ValidateMyChannelAsync : LightUnitTestCase<ValidateMyChannelAsync, Fixture>
    {
        public ValidateMyChannelAsync(Fixture fixture) : base(fixture) { }


        [Theory]
        [InlineData("user3", "email", "49437", "jdedeus3@members.com")]
        [InlineData("user3", "phone", "54783", "+55 (99) 99697-8956")]
        public void Success(string role, string channelType, string validationOTP, string validatedData)
        {
            var response = Fixture.Api
                                           .WithRole(role)
                                           .Put<DomainResponse>($"me/channel/{channelType}/validate?validationOTP={validationOTP}");

            var domainResponse = response.Content;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var resultProfile = domainResponse.Payload.ToObject<ProfileVM>();

            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.Equal(validatedData, channelType == ChannelType.Email.Code ? resultProfile.Email : resultProfile.Phone);
        }
    }
}

