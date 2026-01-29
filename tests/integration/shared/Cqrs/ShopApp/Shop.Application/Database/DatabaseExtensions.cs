using Shop.Domain.Stock;

namespace Shop.Application.Database;

public static class DatabaseExtensions
{
    public static async Task SeedAsync(ApplicationDbContext applicationDbContext)
    {
        var productStocks = new List<ProductStock>
        {
            new ProductStock("Apples", 100),
            new ProductStock("Oranges", 200),
            new ProductStock("Bananas", 300),
            new ProductStock("Kiwis", 150),
            new ProductStock("Pears", 200)
        };

        applicationDbContext.ProductStocks.AddRange(productStocks);
        await applicationDbContext.SaveChangesAsync();
    }
}
