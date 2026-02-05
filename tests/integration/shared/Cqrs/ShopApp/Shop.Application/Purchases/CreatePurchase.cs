
using Ardalis.Result;
using Cqrs.Messaging;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Database;
using Shop.Domain.Purchases;

namespace Shop.Application.Purchases;

public record CreatePurchaseCommand(List<ProductPurchase> ProductPurchases) : ICommand<Guid>;
public record ProductPurchase(string ProductName, int PurchaseQuantity) : ICommand<Guid>;


public class CreatePurchaseCommandValidator : AbstractValidator<CreatePurchaseCommand>
{
    public CreatePurchaseCommandValidator()
    {
        RuleForEach(c => c.ProductPurchases)
            .SetValidator(new ProductPurchaseValidator());
    }

    private class ProductPurchaseValidator : AbstractValidator<ProductPurchase>
    {
        public ProductPurchaseValidator()
        {
            RuleFor(pp => pp.ProductName)
                .NotEmpty().WithMessage("Product name must not be empty.");
            RuleFor(pp => pp.PurchaseQuantity)
                .GreaterThan(0).WithMessage("Purchase quantity must be greater than zero.");
        }
    }

}

public class CreatePurchaseCommandHandler(ApplicationDbContext dbContext) : CommandHandler<CreatePurchaseCommand, Guid>
{
    public override async Task<Result<Guid>> HandleAsync(CreatePurchaseCommand command, CancellationToken cancellationToken)
    {
        var availableStock = await dbContext.ProductStocks
            .Where(ps => command.ProductPurchases.Select(pp => pp.ProductName).Contains(ps.ProductName))
            .ToListAsync(cancellationToken);
        
        var soldProducts = new List<SoldProduct>();
        foreach (var productPurchase in command.ProductPurchases)
        {
            var stock = availableStock.FirstOrDefault(s => s.ProductName == productPurchase.ProductName);
            if (stock is null)
                return Result<Guid>.Error($"Product with ID {productPurchase.ProductName} does not exist.");
            
            if (!stock.IsInStock(productPurchase.PurchaseQuantity))
                return Result<Guid>.Error($"Not enough stock to purchase Product: {productPurchase.ProductName}");

            soldProducts.Add(new SoldProduct
            {
                ProductName = productPurchase.ProductName,
                Quantity = productPurchase.PurchaseQuantity
            });
        }
        var newPurchase = new Purchase(soldProducts);
        dbContext.Add(newPurchase);
        return Result<Guid>.Created(newPurchase.Id);
    }
}

