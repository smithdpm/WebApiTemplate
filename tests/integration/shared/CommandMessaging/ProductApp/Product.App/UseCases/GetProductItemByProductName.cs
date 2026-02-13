using Ardalis.Result;
using Cqrs.Operations.Queries;
using Product.App.Model;
using Product.App.Model.Specification;
using SharedKernel.Database;

namespace Product.App.UseCases;


public record GetProductItemByProductNameQuery 
    (string ProductName)
    : IQuery<ProductItem>;



public class GetProductItemByProductNameQueryHandler(IRepository<ProductItem> repository) : QueryHandler<GetProductItemByProductNameQuery, ProductItem>
{
    public override async Task<Result<ProductItem>> HandleAsync(GetProductItemByProductNameQuery query, CancellationToken cancellationToken)
    {
        var spec = new ProductItemByProductNameSpec(query.ProductName);
        var result = await repository.FirstOrDefaultAsync(spec, cancellationToken);
        if (result == null)
            return Result.NoContent();

        return Result.Success(result);
    }
}