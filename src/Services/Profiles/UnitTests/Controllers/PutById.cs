using Liquid.Base;
using Liquid.Domain;
using Liquid.Domain.Test;
using System.Net;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class PutById : LightUnitTestCase<PutById, Fixture>
    {
        public PutById(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("userPutById")]
        public void Success(string testId)
        {
            var testData = LoadTestData<DomainResponse>(testId);

            var input = testData.Input;
            var id = input.Property("id").AsString();
            var payload = input.Property("payload").ToJsonDocument();

            var output = testData.Output.Payload;

            var expectedId = output.Property("id").AsString();
            var expectedName = output.Property("name").AsString();

            var response = Fixture.Api.Put<DomainResponse>(id, payload);
            var domainResponse = response.Content;
            var resultId = domainResponse.Payload.Property("id").AsString();
            var name = domainResponse.Payload.Property("name").AsString();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.Equal(expectedId, resultId);
            Assert.Equal(expectedName, name);
        }

        [Theory]
        [InlineData("profPutByIdNoContent")]
        public void UserNoContent(string testId)
        {
            var input = LoadTestData<DomainResponse>(testId).Input;
            var id = input.Property("id").AsString();
            var payload = input.Property("payload").ToJsonDocument();

            var response = Fixture.Api.Put(id, payload);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Theory]
        [InlineData("user1")]
        public void Unauthorized(string role)
        {
            var response = Fixture.Api.WithRole(role).Put("anyId");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
