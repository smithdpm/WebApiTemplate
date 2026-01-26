using WebApiTemplate.IntegrationTests.TestCollections.Environments;

namespace WebApiTemplate.IntegrationTests.TestCollections;

[CollectionDefinition("IntegrationTestCollection")]
public class IntegrationTestCollection: ICollectionFixture<IntegrationTestEnvironment>
{
}
