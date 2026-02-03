using Application.Cars.Create;
using Ardalis.Result;
using Cqrs.Events.IntegrationEvents;
using Cqrs.Messaging;

namespace Application.Cars.CarBought;
public class CarBoughtEventHandler(ICommandHandler<CreateCarCommand, Guid> handler) : IntegrationEventHandler<CarBoughtIntegrationEvent>
{
    public override async Task<Result> HandleAsync(CarBoughtIntegrationEvent carBoughtIntegrationEvent, CancellationToken cancellationToken = default)
    {
        var createCarCommand = new CreateCarCommand(
            Model: carBoughtIntegrationEvent.Model,
            Make: carBoughtIntegrationEvent.Make,
            Year: carBoughtIntegrationEvent.Year,
            Mileage: carBoughtIntegrationEvent.Mileage,
            Price: carBoughtIntegrationEvent.BuyPrice * 1.2m);

        Result<Guid> result = await handler.HandleAsync(createCarCommand, cancellationToken);

        return Result.Success();
    }
}
