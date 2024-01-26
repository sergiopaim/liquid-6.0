using Liquid.Base;
using Liquid.Domain;
using Liquid.Domain.Test;
using Liquid.Platform;
using System.Linq;
using System.Net;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class DeleteMe : LightUnitTestCase<DeleteMe, Fixture>
    {
        public DeleteMe(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("userToDelete", "bf2dddfa-aa21-4b67-99ba-8125d4bfa109")]
        public void Success(string role, string id)
        {
            var response = Fixture.Api.WithRole(role).Delete<DomainResponse>("me?feedback=this is a generic feedback");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var domainResponse = response.Content;

            Assert.False(CriticHandler.FromResponse(domainResponse).HasBusinessErrors);

            var msg = Fixture.MessageBus.InterceptedMessages.OfType<ProfileMSG>().FirstOrDefault();

            Assert.Equal(ProfileCMD.Delete.Code, msg.CommandType);
            Assert.Equal(id, msg.Id);
            Assert.Equal(role, msg.Name);
        }

        [Theory]
        [InlineData("generalAdmin")]
        public void Forbidden(string role)
        {
            var response = Fixture.Api.WithRole(role).Delete<DomainResponse>("me");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
