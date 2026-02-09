using Ardalis.Result;
using Cqrs.Messaging;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Database;
using Shop.Domain.Aggregates.Stock;

namespace Shop.Application.Stock;


public record GetStockByProductNameQuery(string ProductName) : IQuery<ProductStock>;

public class GetStockByProductNameQueryHandler(ApplicationDbContext applicationDbContext) : IQueryHandler<GetStockByProductNameQuery, ProductStock>
{
    public async Task<Result<ProductStock>> Handle(GetStockByProductNameQuery query, CancellationToken cancellationToken)
    {
        var result = await applicationDbContext.ProductStocks
            .Where(ps => ps.ProductName == query.ProductName)
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
            return Result.NoContent();

        return Result.Success(result);
    }
}
