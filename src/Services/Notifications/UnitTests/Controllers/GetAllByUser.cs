using Liquid.Base;
using Liquid.Domain;
using Liquid.Platform;
using Liquid.Domain.Test;
using Microservice.ViewModels;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class GetAllByUser : LightUnitTestCase<GetAllByUser, Fixture>
    {
        public GetAllByUser(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("d084740b-9593-4727-be8d-bb5f1f716921")]
        public void Success(string userId)
        {
            var response = Fixture.Api.Get<DomainResponse>($"user/{userId}");
            var domainResponse = response.Content;
            var all = domainResponse.Payload.ToObject<List<HistoryVM>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.Equal(3, all.Count);
        }
    }
}
