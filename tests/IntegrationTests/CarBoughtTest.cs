using Application.Cars;
using Application.Cars.CarBought;
using SharedKernel.Events.IntegrationEvents;
using Shouldly;
using System.Net.Http.Json;


namespace IntegrationTests;

[Collection("WebApiTests")]
public class CarBoughtTest
{

    private WebApiFixture _fixture { get; }
    private ITestOutputHelper OutputHelper { get; }

    public CarBoughtTest(WebApiFixture webApiFixture, ITestOutputHelper outputHelper)
    {
        _fixture = webApiFixture;

        OutputHelper = outputHelper;
        _fixture.SetOutputHelper(OutputHelper);
    }


    [Fact]
    public async Task CarBoughtIntegrationEvent()
    {
        // Arrange
        //var cancellationToken = TestContext.Current.CancellationToken;
        var client = _fixture.CreateClient();
        var serviceBusSender = new ServiceBusMessageSender(_fixture.GetServiceBusConnectionString());
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
        await serviceBusSender.SendMessageAsync(events, "warehouse-events");
        Thread.Sleep(1000); // Wait for the message to be processed

        var boughtCar = await client.GetFromJsonAsync<List<CarDto>>($"/cars?make={carBoughtEvent.Make}");

        // Assert
        boughtCar.ShouldNotBeNull();
        boughtCar.First().Make.ShouldBe(carBoughtEvent.Make);
        boughtCar.First().Model.ShouldBe(carBoughtEvent.Model);
        boughtCar.First().Year.ShouldBe(carBoughtEvent.Year);
        boughtCar.First().Mileage.ShouldBe(carBoughtEvent.Mileage);
        boughtCar.First().Price.ShouldBe(carBoughtEvent.BuyPrice*1.2m, 0.01m);
    }
}
