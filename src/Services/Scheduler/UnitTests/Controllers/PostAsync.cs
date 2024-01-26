using Liquid.Domain.Test;
using Xunit;

namespace UnitTests.Controllers
{
    [Collection("General")]
    public class PostAsync : LightUnitTestCase<PostAsync, Fixture>
    {
        public PostAsync(Fixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("repPost01")]
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
#pragma warning disable IDE0060 // Remove unused parameter
        public void Success(string testId) { }
#pragma warning restore IDE0060 // Remove unused parameter
    }
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
}
