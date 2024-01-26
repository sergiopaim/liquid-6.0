using Liquid.Base;
using Liquid.Domain;
using Liquid.Platform;
using Liquid.Domain.Test;
using System;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class ClearAllMineViewed : LightUnitTestCase<ClearAllMineViewed, Fixture>
    {
        public ClearAllMineViewed(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("5e179f25-8d4d-43e9-b358-5320e740329f")]
        public void Success(string userId)
        {
            var response = Fixture.Api.WithRole(userId).Delete<DomainResponse>("mine");
            var domainResponse = response.Content;
            var deleted = domainResponse.Payload.ToObject<List<NotificationVM>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.Single(deleted);

            response = Fixture.Api.WithRole(userId).Get<DomainResponse>("mine");
            domainResponse = response.Content;
            var fressAll = domainResponse.Payload.ToObject<List<NotificationVM>>();

            Assert.Equal(2, fressAll.Count);

            fressAll.ForEach(f => Assert.Equal(DateTime.MinValue, f.ViewedAt));
        }
    }
}
