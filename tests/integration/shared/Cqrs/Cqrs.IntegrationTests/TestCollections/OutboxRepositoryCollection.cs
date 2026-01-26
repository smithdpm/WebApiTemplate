using Cqrs.IntegrationTests.TestCollections.Environments;

namespace Cqrs.IntegrationTests.TestCollections;

[CollectionDefinition(nameof(OutboxRepositoryCollection))]
public class OutboxRepositoryCollection : ICollectionFixture<OutboxRepositoryEnvironment>
{
}