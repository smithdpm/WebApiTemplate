using SharedKernel.Database;

namespace Product.App.Model;

public class ProductItem: IAggregateRoot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }

    public decimal RetailPrice { get; set; }
    public int TotalSold { get; set; } = 0;
    public int TotalShipped { get; set; } = 0;

    public ProductItem(string name, string description, string category, decimal retailPrice)
    {
        Name = name;
        Description = description;
        Category = category;
        RetailPrice = retailPrice;
    }

    public void UpdateTotalShipped(int quantity)
    {
        TotalShipped += quantity;
    }
}
