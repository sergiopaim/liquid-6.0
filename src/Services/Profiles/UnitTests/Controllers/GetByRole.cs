using Liquid.Base;
using Liquid.Domain;
using Liquid.Domain.Test;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class GetByRole : LightUnitTestCase<GetByRole, Fixture>
    {
        public GetByRole(Fixture fixture) : base(fixture) { }
        [Theory]
        [InlineData("generalAdmin", 1)]
        public void Success(string roleName, int quantity)
        {
            var response = Fixture.Api.Get<DomainResponse>($"byRole/{roleName}");
            var domainResponse = response.Content;
            var count = domainResponse.Payload.ToObject<List<JsonDocument>>().Count;

            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.Equal(quantity, count);
        }

        [Theory]
        [InlineData("Guest")]
        public void Unauthorized(string role)
        {
            var response = Fixture.Api.WithRole(role).Get("byRole/fieldManager");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
