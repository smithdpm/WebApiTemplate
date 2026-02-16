using Cqrs.Operations.Commands;

namespace Application.Cars.SellCar;

public sealed record SellCarCommand(
    Guid CarId,
    decimal SalePrice) : ICommand;

