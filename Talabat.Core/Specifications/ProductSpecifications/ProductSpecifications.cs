using Talabat.Core.Models;
using Talabat.Core.Specification;

namespace Talabat.Core.Specifications.ProductSpecifications;

public class ProductSpecifications : BaseSpecifications<Product>
{
    public ProductSpecifications(ProductsSpecParams specParams)
        : base(p =>
            (string.IsNullOrEmpty(specParams.Search) || p.Name.ToLower().Contains(specParams.Search)))
    {
        AddIncludes();
        switch (specParams.sort)
        {
            case "priceAsc":
                AddOrderBy(p => p.Price);
                break;
            case "priceDesc":
                AddOrderByDesc(p => p.Price);
                break;
            default:
                AddOrderBy(p => p.Name);
                break;
        }
        ApplyPagination(specParams.PageSize * (specParams.PageIndex - 1), specParams.PageSize);
    }

    public ProductSpecifications(int id)
        : base(p => p.Id == id)
    {
        AddIncludes();
    }

    private void AddIncludes()
    {
    }
}
