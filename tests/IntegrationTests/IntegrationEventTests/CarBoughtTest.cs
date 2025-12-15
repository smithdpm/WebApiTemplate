using Application.Cars;
using Application.Cars.CarBought;
using IntegrationTests.TestCollections.Environments;
using Polly;
using SharedKernel.Events.IntegrationEvents;
using Shouldly;
using System.Net.Http.Json;


namespace IntegrationTests.IntegrationEventTests;

[Collection("IntegrationTestCollection")]
public class CarBoughtTest
{

    private IntegrationTestEnvironment _environment { get; }
    private ITestOutputHelper OutputHelper { get; }

    public CarBoughtTest(IntegrationTestEnvironment environment, ITestOutputHelper outputHelper)
    {
        _environment = environment;

        OutputHelper = outputHelper;
        _environment.WebApi.SetOutputHelper(OutputHelper);
    }


    [Fact]
    public async Task CarBoughtIntegrationEvent_ShouldCreateCarRecordInSystem()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _environment.WebApi.CreateClient();
        var serviceBusSender = new ServiceBusMessageSender(_environment.ServiceBus.GetConnectionString());
        Random random = new Random();

        var carBoughtEvent = new CarBoughtIntegrationEvent(WarehouseCarId: Guid.NewGuid(),
            WarehouseId: Guid.NewGuid(),
            Model: "Model S",
            Make: "TestMake" + random.Next(5).ToString(),
            Year: 2024,
            Mileage: 10,
            BuyPrice: 79999.99m
            );

        var events = new List<IntegrationEventBase>();
        events.Add(carBoughtEvent);
        
        // Act
        await serviceBusSender.SendMessageAsync(events, "warehouse-events", cancellationToken);

        var boughtCar = await WaitForCarsAsync(client, carBoughtEvent.Make, cancellationToken);

        // Assert
        boughtCar.ShouldNotBeNull();
        boughtCar.First().Make.ShouldBe(carBoughtEvent.Make);
        boughtCar.First().Model.ShouldBe(carBoughtEvent.Model);
        boughtCar.First().Year.ShouldBe(carBoughtEvent.Year);
        boughtCar.First().Mileage.ShouldBe(carBoughtEvent.Mileage);
        boughtCar.First().Price.ShouldBe(carBoughtEvent.BuyPrice*1.2m, 0.01m);
    }

    private static Task<List<CarDto>> WaitForCarsAsync(HttpClient client, string make, CancellationToken cancellationToken)
    {
        var retryPolicy = Policy<List<CarDto>?>
            .Handle<HttpRequestException>()
            .OrResult(cars => cars is null || !cars.Any())
            .WaitAndRetryAsync(50, _ => TimeSpan.FromMilliseconds(200));

        var result = retryPolicy.ExecuteAsync(
            () => client.GetFromJsonAsync<List<CarDto>>($"/cars?make={make}", cancellationToken));

        return result!;
    }
}
