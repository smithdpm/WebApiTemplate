
using Product.App.Model;

namespace Product.App.Database;

public static class DatabaseExtensions
{
    public static async Task SeedAsync(ApplicationDbContext applicationDbContext)
    {
        var productItems = new List<ProductItem>
        {
            new ProductItem("Apples","Green & crunchy", "Fruit", 0.65m),
            new ProductItem("Oranges", "Orange & juicy", "Fruit", 0.55m),
            new ProductItem("Bananas", "Yellow & soft", "Fruit", 0.25m),
            new ProductItem("Kiwis", "Brown & fuzzy", "Fruit", 0.85m),
            new ProductItem("Pears", "Green & sweet", "Fruit", 0.60m)
        };

        applicationDbContext.ProductItems.AddRange(productItems);
        await applicationDbContext.SaveChangesAsync();
    }
}
