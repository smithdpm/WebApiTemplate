using Application.Abstractions.Database;
using Application.Abstractions.Messaging;
using Domain.Cars;
using SharedKernel;

namespace Application.Cars.Create
{
    public class CreateCarHandler(IRepository<Car> repository) : ICommandHandler<CreateCarCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateCarCommand command, CancellationToken cancellationToken)
        {
            var newCar = new Car(command.Make, command.Model, command.Year, command.Mileage, command.Price);

            await repository.AddAsync(newCar, cancellationToken);

            return newCar.Id;
        }
    }
}
