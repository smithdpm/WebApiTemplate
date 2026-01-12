using ComponentTests.TestCollections.Environments;

namespace ComponentTests.TestCollections;

[CollectionDefinition(nameof(OutboxRepositoryCollection))]
public class OutboxRepositoryCollection : ICollectionFixture<OutboxRepositoryEnvironment>
{
}