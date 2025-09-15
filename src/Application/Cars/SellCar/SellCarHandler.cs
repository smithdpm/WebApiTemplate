
using Ardalis.Result;
using Domain.Cars;
using SharedKernel.Database;
using SharedKernel.Messaging;

namespace Application.Cars.SellCar;

internal class SellCarHandler(IRepository<Car> repository) : ICommandHandler<SellCarCommand, bool>
{
    public async Task<Result<bool>> Handle(SellCarCommand command, CancellationToken cancellationToken)
    {
        var car = await repository.GetByIdAsync(command.CarId, cancellationToken);

        if (car is null)
            return Result<bool>.NotFound($"Car with id: {command.CarId} not found.");
        
        car.SellCar(command.SalePrice);

        await repository.UpdateAsync(car, cancellationToken);

        return true;
    }
}

