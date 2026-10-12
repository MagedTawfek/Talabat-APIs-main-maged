using Microsoft.EntityFrameworkCore;
using Talabat.Core.Models;
using Talabat.Repository;
using Talabat.Repository.Data;
using Talabat.Services;

namespace Talabat.Tests;

public class ProductCreationTests
{
    [Fact]
    public async Task CreateProduct_AssignsDatabaseId()
    {
        await using var context = CreateContext();
        var service = new ProductService(new UnitOfWork(context));
        var product = await service.CreateProductAsync(new Product
        {
            Name = "Test product",
            Description = "A product used by the test",
            PictureUrl = "/images/test.png",
            Price = 12.50m
        });

        Assert.NotNull(product);
        Assert.True(product.Id > 0);
        Assert.Equal(1, await context.Products.CountAsync());
    }

    private static StoreContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StoreContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StoreContext(options);
    }
}
