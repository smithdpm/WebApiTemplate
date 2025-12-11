
using Ardalis.Result;
using Domain.Cars;
using SharedKernel.Database;
using SharedKernel.Messaging;

namespace Application.Cars.SellCar;

internal class SellCarHandler(IRepository<Car> repository) : ICommandHandler<SellCarCommand>
{
    public async Task<Result> Handle(SellCarCommand command, CancellationToken cancellationToken)
    {
        var car = await repository.GetByIdAsync(command.CarId, cancellationToken);

        if (car is null)
            return Result.NotFound($"Car with id: {command.CarId} not found.");
        
        var result = car.SellCar(command.SalePrice);

        if(result.IsSuccess)
            await repository.UpdateAsync(car, cancellationToken);

        return result;
    }
}

