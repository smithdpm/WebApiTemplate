
using Application.Cars.IntegrationEvents;
using Ardalis.Result;
using Domain.Cars;
using SharedKernel.Database;
using Cqrs.Messaging;
using Cqrs.Events.IntegrationEvents;

namespace Application.Cars.SellCar;

internal class SellCarHandler(IRepository<Car> repository) : HasIntegrationEvents, ICommandHandler<SellCarCommand>
{
    public async Task<Result> Handle(SellCarCommand command, CancellationToken cancellationToken)
    {
        var car = await repository.GetByIdAsync(command.CarId, cancellationToken);

        if (car is null)
            return Result.NotFound($"Car with id: {command.CarId} not found.");
        
        var result = car.SellCar(command.SalePrice);

        if(result.IsSuccess)
        {
            AddIntegrationEvent("cars-events", new CarSoldIntegrationEvent(car.Id, (DateTime)car.SoldAt!, (decimal)car.SoldPrice!));
            await repository.UpdateAsync(car, cancellationToken);
        }           

        return result;
    }
}

