using Talabat.Core.Models;
using Talabat.Core.Specifications.ProductSpecifications;

namespace Talabat.Core.Services.Contract;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetProductsAsync(ProductsSpecParams specParams);

    Task<Product?> GetProductAsync(int productId);

    Task<int> GetCountAsync(ProductsSpecParams specParams);

    Task<Product> CreateProductAsync(Product product);

}
