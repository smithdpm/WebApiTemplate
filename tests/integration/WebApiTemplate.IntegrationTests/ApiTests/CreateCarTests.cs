using Application.Cars.Create;
using WebApiTemplate.IntegrationTests.TestCollections.Environments;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;


namespace WebApiTemplate.IntegrationTests.ApiTests;

[Collection("IntegrationTestCollection")]
public class CreateCarTests
{
    private readonly IntegrationTestEnvironment _environment;
    private ITestOutputHelper OutputHelper { get; }

    public CreateCarTests(IntegrationTestEnvironment environment, ITestOutputHelper outputHelper)      
    {
        _environment = environment;

        OutputHelper = outputHelper;
        _environment.WebApi.SetOutputHelper(OutputHelper);
    }
    [Fact]
    public async Task CreateCarRequest_ShouldReturnSucessfullyReturnId_WhenValid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var client = _environment.WebApi.CreateClient();
        var createCarRequest = new CreateCarCommand("Toyota", "Corolla", 2020, 15000, 20000m);

        // Act
        using var response = await client.PostAsJsonAsync("api/cars", createCarRequest, cancellationToken);
        using var createdJson = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        createdJson!.RootElement.GetGuid().ShouldNotBe(Guid.Empty);
    }


}
