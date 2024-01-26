using Liquid.Base;
using Liquid.Domain;
using Liquid.Domain.Test;
using Microservice.ViewModels;
using System.Net;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class GetUserIdById : LightUnitTestCase<GetUserIdById, Fixture>
    {
        public GetUserIdById(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("3aa9455a-ec1c-4178-8bfa-97d8891f1856", "c1d17649-4a01-41e9-b12f-2e56e403e8a7")]
        public void Success(string userId, string id)
        {
            var response = Fixture.Api.Get<DomainResponse>($"{id}/userBasicInfo");
            var domainResponse = response.Content;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var user = domainResponse.Payload.ToObject<BasicUserInfoVM>();
            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.Equal(userId, user.Id);
        }
    }
}
