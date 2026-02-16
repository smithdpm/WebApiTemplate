using Ardalis.Specification;

namespace Product.App.Model.Specification;

public class ProductItemByCategorySpec: Specification<ProductItem>
{
    public ProductItemByCategorySpec(string category)
        => Query.Where(p => p.Category == category);
}
