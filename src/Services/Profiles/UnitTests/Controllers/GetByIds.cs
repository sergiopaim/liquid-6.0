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
    public class GetByIds : LightUnitTestCase<GetByIds, Fixture>
    {
        public GetByIds(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData(new string[] { "c1183078-3c0f-4767-9f9c-ade0531fd139" }, 1)]
        [InlineData(new string[] { "c1183078-3c0f-4767-9f9c-ade0531fd139", "6c5f063d-22f7-4635-9849-02ea54d9b9cc" }, 2)]
        [InlineData(new string[] { "c1183078-3c0f-4767-9f9c-ade0531fd139", "21cc120c-7874-4a89-b971-fd7d756abcb2" }, 1)]
        [InlineData(new string[] { "" }, 0)]
        public void Success(string[] ids, int quantity)
        {
            var response = Fixture.Api.Get<DomainResponse>($"byIds?ids={string.Join("&ids=", ids)}");
            var domainResponse = response.Content;
            var count = domainResponse.Payload.ToObject<List<JsonDocument>>().Count;

            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.True(count == quantity);
        }

        [Theory]
        [InlineData("Guest")]
        public void Unauthorized(string role)
        {
            var response = Fixture.Api.WithRole(role).Get("byIds?ids=c1183078-3c0f-4767-9f9c-ade0531fd139");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
