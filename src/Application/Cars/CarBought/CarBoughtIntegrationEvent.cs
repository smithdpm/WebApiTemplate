using SharedKernel.Events.IntegrationEvents;

namespace Application.Cars.CarBought;
public record CarBoughtIntegrationEvent
(
    Guid WarehouseCarId,
    Guid WarehouseId,
    string Model,
    string Make,
    int Year,
    int Mileage,
    decimal BuyPrice
) : IntegrationEventBase;
