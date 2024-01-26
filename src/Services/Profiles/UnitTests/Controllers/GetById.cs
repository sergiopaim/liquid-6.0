using Liquid.Base;
using Liquid.Domain;
using Liquid.Domain.Test;
using System.Net;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class GetById : LightUnitTestCase<GetById, Fixture>
    {
        public GetById(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("profGetById01")]
        public void Success(string testId)
        {
            var testData = LoadTestData<DomainResponse>(testId);

            var input = testData.Input;
            var id = input.Property("id").AsString();

            var expectedId = testData.Output.Payload.Property("id").AsString();

            var response = Fixture.Api.Get<DomainResponse>($"{id}");
            var domainResponse = response.Content;
            var resultId = domainResponse.Payload.Property("id").AsString();

            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);
            Assert.Equal(expectedId, resultId);
        }

        [Theory]
        [InlineData("profGetByIdNoContent")]
        public void UserNoContent(string testId)
        {
            var id = LoadTestData(testId).Input.Property("id").AsString();
            var response = Fixture.Api.Get($"{id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
