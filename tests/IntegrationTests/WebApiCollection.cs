

namespace IntegrationTests;

[CollectionDefinition(Name)]
public sealed class WebApiCollection: ICollectionFixture<WebApiFixture>
{
    public const string Name = "WebApiTests";
}
