using Xunit;

namespace UnitTests
{
    [CollectionDefinition("General")]
    public class Collection : ICollectionFixture<Fixture> { }
}
