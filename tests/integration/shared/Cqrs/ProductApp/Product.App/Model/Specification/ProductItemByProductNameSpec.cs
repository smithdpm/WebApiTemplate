using Ardalis.Specification;

namespace Product.App.Model.Specification;

public class ProductItemByProductNameSpec: Specification<ProductItem>, ISingleResultSpecification<ProductItem>
{
    public ProductItemByProductNameSpec(string productName)
        => Query.Where(p => p.Name == productName);
}
