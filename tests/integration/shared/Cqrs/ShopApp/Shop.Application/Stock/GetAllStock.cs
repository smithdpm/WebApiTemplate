using Ardalis.Result;
using Cqrs.Messaging;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Database;
using Shop.Domain.Aggregates.Stock;

namespace Shop.Application.Stock;


public record GetAllStockQuery(int MaximumProductsReturned): IQuery<List<ProductStock>>;

public class GetAllStockQueryHandler(ApplicationDbContext applicationDbContext) : QueryHandler<GetAllStockQuery, List<ProductStock>>
{
    public override async Task<Result<List<ProductStock>>> HandleAsync(GetAllStockQuery query, CancellationToken cancellationToken)
    {
        return await applicationDbContext.ProductStocks
            .Take(query.MaximumProductsReturned) 
            .ToListAsync(cancellationToken);
    }
}
