using Application.Cars.Create;
using Ardalis.Result;
using Domain.Cars;
using SharedKernel.Database;
using SharedKernel.Events.IntegrationEvents;
using SharedKernel.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Cars.CarBought;
public class CarBoughtEventHandler(ICommandHandler<CreateCarCommand, Guid> handler) : IIntegrationEventHandler<CarBoughtIntegrationEvent>
{
    public async Task HandleAsync(CarBoughtIntegrationEvent carBoughtIntegrationEvent, CancellationToken cancellationToken = default)
    {
        var createCarCommand = new CreateCarCommand(
            Model: carBoughtIntegrationEvent.Model,
            Make: carBoughtIntegrationEvent.Make,
            Year: carBoughtIntegrationEvent.Year,
            Mileage: carBoughtIntegrationEvent.Mileage,
            Price: carBoughtIntegrationEvent.BuyPrice * 1.2m);

        Result<Guid> result = await handler.Handle(createCarCommand, cancellationToken);
    }
}
