namespace Shop.Domain.Aggregates.Purchases;

public class SoldProduct
{
    public required string ProductName { get; set; }
    public int Quantity { get; set; }
}