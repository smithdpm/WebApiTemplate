using Ardalis.Result;
using Cqrs.Messaging;
using Product.App.Model;
using Product.App.Model.Specification;
using SharedKernel.Database;

namespace Product.App.UseCases;


public record GetProductItemsByCategoryQuery(string Category) : IQuery<List<ProductItem>>;

public class GetProductItemsByCategoryQueryHandler(IRepository<ProductItem> repository) : IQueryHandler<GetProductItemsByCategoryQuery, List<ProductItem>>
{
    public async Task<Result<List<ProductItem>>> Handle(GetProductItemsByCategoryQuery query, CancellationToken cancellationToken)
    {
        var spec = new ProductItemByCategorySpec(query.Category);
        var result = await repository
            .ListAsync(spec, cancellationToken);

        if (result is null || !result.Any())
        {
            return Result<List<ProductItem>>.NoContent();
        }

        return Result<List<ProductItem>>.Success(result);
    }

}
