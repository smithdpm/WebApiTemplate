using Ardalis.Result;
using Cqrs.Operations.Commands;
using Product.App.Events;
using Product.App.Model;
using SharedKernel.Database;

namespace Product.App.UseCases;


public record CreateProdcutItemCommand(
    string Name,
    string Description,
    string Category,
    decimal RetailPrice): ICommand<Guid>;



public class CreateProductItemCommandHandler(IRepository<ProductItem> repository) : CommandHandler<CreateProdcutItemCommand, Guid>
{
    public override async Task<Result<Guid>> HandleAsync(CreateProdcutItemCommand input, CancellationToken cancellationToken)
    {
        var newProduct = new ProductItem(input.Name, input.Description, input.Category, input.RetailPrice);
        await repository.AddAsync(newProduct);
        AddIntegrationEvent(new ProductItemCreatedIntegrationEvent(newProduct.Id, newProduct.Name));
        return Result.Created(newProduct.Id);
    }
}
