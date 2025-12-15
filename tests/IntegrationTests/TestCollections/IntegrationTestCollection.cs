using IntegrationTests.TestCollections.Environments;

namespace IntegrationTests.Fixtures;

[CollectionDefinition("IntegrationTestCollection")]
public class IntegrationTestCollection: ICollectionFixture<IntegrationTestEnvironment>
{
}
