using Ardalis.Result;
using Cqrs.Messaging;
using FluentValidation;
using Shop.Application.Database;
using Shop.Application.IntegrationEvents.Events;

namespace Shop.Application.Stock;

public record UpdateStockCommand(string ProductName, int QuantityToAdd): ICommand;


public class UpdateStockCommandValidator : AbstractValidator<UpdateStockCommand>
{
    public UpdateStockCommandValidator()
    {
        RuleFor(us => us.ProductName)
            .NotEmpty().WithMessage("Product name must not be empty.");
        RuleFor(pp => pp.QuantityToAdd)
               .GreaterThan(0).WithMessage("Quantity to add must be greater than zero.");
    }
}
public class UpdateStockCommandHandler(
    ApplicationDbContext applicationDbContext) : CommandHandler<UpdateStockCommand>
{

    public override async Task<Result> HandleAsync(UpdateStockCommand input, CancellationToken cancellationToken)
    {
        var stockItem = applicationDbContext.ProductStocks
            .FirstOrDefault(s => s.ProductName == input.ProductName);

        if (stockItem is null)
            return Result.NotFound($"Stock item for product '{input.ProductName}' not found.");

        stockItem.TotalInStock += input.QuantityToAdd;
        AddIntegrationEvent(new StockAddedIntegrationEvent(input.ProductName, input.QuantityToAdd));
        return Result.Success();
    }
}
