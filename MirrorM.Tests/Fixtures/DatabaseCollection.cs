using FishingTourServerTests.Tests.DataAccessLayer.Fixtures;

namespace MirrorM.Tests.Fixtures
{
    [CollectionDefinition(NAME)]
    public class DatabaseCollection : ICollectionFixture<TestDatabaseCreationFixture>
    {
        public const string NAME = "Database collection";
    }
}
