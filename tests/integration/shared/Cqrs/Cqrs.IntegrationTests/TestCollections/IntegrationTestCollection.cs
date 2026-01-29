using Cqrs.IntegrationTests.TestCollections.Environments;

namespace Cqrs.IntegrationTests.TestCollections;

[CollectionDefinition("IntegrationTestCollection")]
public class IntegrationTestCollection: ICollectionFixture<IntegrationTestEnvironment>
{
}
