
namespace Shop.Domain.Stock;

public class ProductStock
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProductName { get; set; }
    public int TotalInStock { get; set; }

    public ProductStock(string productName, int totalInStock)
    {
        ProductName = productName;
        TotalInStock = totalInStock;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        TotalInStock += quantity;
    }
    public void RemoveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (quantity > TotalInStock)
            throw new InvalidOperationException("Insufficient stock available.");
        TotalInStock -= quantity;
    }

    public bool IsInStock(int quantity) => TotalInStock >= quantity;

    public bool IsLowStock() => TotalInStock <= 10;
}
