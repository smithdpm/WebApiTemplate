using Ardalis.Result;
using Cqrs.Messaging;
using Shop.Application.Database;

namespace Shop.Application.Stock;

public record UpdateStockCommand(string ProductName, int QuantityToAdd): ICommand;


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
        return Result.Success();
    }
}
