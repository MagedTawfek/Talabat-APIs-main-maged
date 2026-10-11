using Talabat.Core.Models;

namespace Talabat.Core.Repositories.Contract;

public interface IBasketRepository
{
    Task<CustomerBasket?> GetBasketAsync(string basketId, string ownerId);
    Task<CustomerBasket?> UpdateBasketAsync(CustomerBasket basket, string ownerId);
    Task<bool> DeleteBasketAsync(string basketId, string ownerId);
}
