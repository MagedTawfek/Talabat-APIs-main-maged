using Talabat.Core;
using Talabat.Core.Models;
using Talabat.Core.Services.Contract;
using Talabat.Core.Specifications.ProductSpecifications;

namespace Talabat.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<IReadOnlyList<Product>> GetProductsAsync(ProductsSpecParams specParams)
    {
        var spec = new ProductSpecifications(specParams);  
        var products = await _unitOfWork.Repository<Product>().GetAllWithSpecAsync(spec);
        return products;
    }

    public async Task<Product?> GetProductAsync(int productId)
    {
        var spec = new ProductSpecifications(productId);
        var product = await _unitOfWork.Repository<Product>().GetWithSpecAsync(spec);
        return product;
    }

    public async Task<int> GetCountAsync(ProductsSpecParams specParams)
    {
        var countSpec = new ProductsWithFiltrationForCountSpec(specParams);
        var count = await _unitOfWork.Repository<Product>().GetCountAsync(countSpec);
        return count;
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        await _unitOfWork.Repository<Product>().AddAsync(product);
        await _unitOfWork.CompleteAsync();
        return product;
    }

}
