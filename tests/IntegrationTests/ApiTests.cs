using Application.Cars.Create;
using Microsoft.AspNetCore.Mvc.Testing;
using Org.BouncyCastle.Tls;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;


namespace IntegrationTests;

[Collection("WebApiTests")]
public class ApiTests
{

    private WebApiFixture _fixture { get; }
    private ITestOutputHelper OutputHelper { get; }

    public ApiTests(WebApiFixture webApiFixture, ITestOutputHelper outputHelper)
    {
        _fixture = webApiFixture;

        OutputHelper = outputHelper;
        _fixture.SetOutputHelper(OutputHelper);
    }
    [Fact]
    public async Task Test1()
    {
        // Arrange
        //var cancellationToken = TestContext.Current.CancellationToken;
        var client = _fixture.CreateClient();
        var createCarRequest = new CreateCarCommand("Toyota", "Corolla", 2020, 15000, 20000m);

        // Act
        using var response = await client.PostAsJsonAsync("/cars", createCarRequest);
        using var createdJson = await response.Content.ReadFromJsonAsync<JsonDocument>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        createdJson!.RootElement.GetGuid().ShouldNotBe(Guid.Empty);
    }


}
