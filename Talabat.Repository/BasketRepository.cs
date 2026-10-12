using Microsoft.EntityFrameworkCore;
using Talabat.Core.Models;
using Talabat.Core.Repositories.Contract;
using Talabat.Repository.Data;

namespace Talabat.Repository;

public class BasketRepository : IBasketRepository
{
    private readonly StoreContext _context;

    public BasketRepository(StoreContext context) => _context = context;

    public Task<CustomerBasket?> GetBasketAsync(string basketId, string ownerId) =>
        _context.CustomerBaskets.Include(basket => basket.items)
            .FirstOrDefaultAsync(basket => basket.id == basketId && basket.OwnerId == ownerId);

    public async Task<CustomerBasket?> UpdateBasketAsync(CustomerBasket basket, string ownerId)
    {
        var existingBasket = await _context.CustomerBaskets
            .Include(current => current.items)
            .FirstOrDefaultAsync(current => current.id == basket.id);

        if (existingBasket is not null && existingBasket.OwnerId is not null && existingBasket.OwnerId != ownerId)
        {
            return null;
        }

        basket.OwnerId = ownerId;
        if (existingBasket is null)
        {
            _context.CustomerBaskets.Add(basket);
        }
        else
        {
            _context.Entry(existingBasket).CurrentValues.SetValues(basket);
            _context.BasketItems.RemoveRange(existingBasket.items);
            existingBasket.items = basket.items;
        }

        await _context.SaveChangesAsync();
        return await GetBasketAsync(basket.id, ownerId);
    }

    public async Task<bool> DeleteBasketAsync(string basketId, string ownerId)
    {
        if (!await RemoveBasketAsync(basketId, ownerId)) return false;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> RemoveBasketAsync(string basketId, string ownerId)
    {
        var basket = await _context.CustomerBaskets
            .FirstOrDefaultAsync(current => current.id == basketId && current.OwnerId == ownerId);
        if (basket is null) return false;

        _context.CustomerBaskets.Remove(basket);
        return true;
    }
}
