using Cqrs.Operations.Commands;

namespace Application.Cars.Create;

public sealed record CreateCarCommand 
    (
        string Model,
        string Make,
        int Year,
        int Mileage,
        decimal Price
    ): ICommand<Guid>;
