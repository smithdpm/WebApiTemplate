using SharedKernel.Database;
using Cqrs.Messaging;
using Ardalis.Result;
using Domain.Cars;

namespace Application.Cars.Create
{
    public class CreateCarHandler(IRepository<Car> repository, ICarFactory factory) : CommandHandler<CreateCarCommand, Guid>
    {
        public override async Task<Result<Guid>> HandleAsync(CreateCarCommand command, CancellationToken cancellationToken)
        {
            var newCar = factory.Create(command.Make, command.Model, command.Year, command.Mileage, command.Price);

            await repository.AddAsync(newCar, cancellationToken);

            return Result<Guid>.Created(newCar.Id);
        }
    }
}
