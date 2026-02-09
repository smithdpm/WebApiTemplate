using Ardalis.Result;
using Cqrs.Messaging;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Database;
using Shop.Domain.Aggregates.Stock;

namespace Shop.Application.Stock;


public record GetAllStockQuery(int MaximumProductsReturned): IQuery<List<ProductStock>>;

public class GetAllStockQueryHandler(ApplicationDbContext applicationDbContext) : IQueryHandler<GetAllStockQuery, List<ProductStock>>
{
    public async Task<Result<List<ProductStock>>> Handle(GetAllStockQuery query, CancellationToken cancellationToken)
    {
        return await applicationDbContext.ProductStocks
            .Take(query.MaximumProductsReturned) 
            .ToListAsync(cancellationToken);
    }
}
