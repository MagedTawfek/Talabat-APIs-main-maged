using System.Text.Json;
using Microsoft.Extensions.Logging;
using Talabat.Core.Models;

namespace Talabat.Repository.Data;

public class StoreContextSeed
{
    public async static Task SeedAsync(StoreContext _dbContext)
    {
        if (!_dbContext.Products.Any())
        {
            var productData = await File.ReadAllTextAsync("../Talabat.Repository/Data/DataSeed/products.json");
            var products = JsonSerializer.Deserialize<List<Product>>(productData);
            if (products is not null && products.Count > 0)
            {

                foreach (var product in products)
                {
                    _dbContext.Set<Product>().Add(product);
                }

                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
