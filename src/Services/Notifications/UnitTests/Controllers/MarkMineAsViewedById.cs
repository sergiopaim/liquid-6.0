using Liquid.Base;
using Liquid.Domain;
using Liquid.Platform;
using Liquid.Domain.Test;
using System;
using System.Net;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class MarkMineAsViewedById : LightUnitTestCase<MarkMineAsViewedById, Fixture>
    {
        public MarkMineAsViewedById(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("3aa9455a-ec1c-4178-8bfa-97d8891f1856", "c1d17649-4a01-41e9-b12f-2e56e403e8a7")]
        public void Success(string userId, string id)
        {
            var response = Fixture.Api.WithRole(userId).Put<DomainResponse>($"mine/{id}");
            var domainResponse = response.Content;
            var one = domainResponse.Payload.ToObject<NotificationVM>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.NotNull(one);

            response = Fixture.Api.WithRole(userId).Get<DomainResponse>($"mine/{id}");
            domainResponse = response.Content;
            var freshOne = domainResponse.Payload.ToObject<NotificationVM>();
            Assert.NotEqual(DateTime.MinValue, freshOne.ViewedAt);
        }
    }
}
