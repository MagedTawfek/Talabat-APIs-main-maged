using Microsoft.EntityFrameworkCore;
using Talabat.Core.Models;
using Talabat.Repository;
using Talabat.Repository.Data;

namespace Talabat.Tests;

public class BasketOwnershipTests
{
    [Fact]
    public async Task BasketCanOnlyBeReadUpdatedOrDeletedByItsOwner()
    {
        var options = new DbContextOptionsBuilder<StoreContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new StoreContext(options);
        var repository = new BasketRepository(context);
        var basket = new CustomerBasket("basket-id")
        {
            items = [new BasketItem { Id = 5, productName = "Tea", PictureUrl = "tea.png", Price = 3, Category = "Drinks", Brand = "A", Quantity = 1 }]
        };

        var saved = await repository.UpdateBasketAsync(basket, "customer-a");
        var foreignRead = await repository.GetBasketAsync("basket-id", "customer-b");
        var foreignUpdate = await repository.UpdateBasketAsync(new CustomerBasket("basket-id"), "customer-b");
        var foreignDelete = await repository.DeleteBasketAsync("basket-id", "customer-b");
        var ownerRead = await repository.GetBasketAsync("basket-id", "customer-a");

        Assert.Equal("customer-a", saved!.OwnerId);
        Assert.Null(foreignRead);
        Assert.Null(foreignUpdate);
        Assert.False(foreignDelete);
        Assert.Equal("customer-a", ownerRead!.OwnerId);
        Assert.Single(ownerRead.items);
    }

    [Fact]
    public async Task LegacyUnownedBasketCanBeClaimedWithoutExposingItsOldContents()
    {
        var options = new DbContextOptionsBuilder<StoreContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new StoreContext(options);
        context.CustomerBaskets.Add(new CustomerBasket("legacy")
        {
            items = [new BasketItem { Id = 9, productName = "Old item", PictureUrl = "old.png", Price = 1, Category = "Old", Brand = "Old", Quantity = 1 }]
        });
        await context.SaveChangesAsync();
        var repository = new BasketRepository(context);

        Assert.Null(await repository.GetBasketAsync("legacy", "customer-b"));
        var claimed = await repository.UpdateBasketAsync(new CustomerBasket("legacy")
        {
            items = [new BasketItem { Id = 4, productName = "New item", PictureUrl = "new.png", Price = 2, Category = "New", Brand = "New", Quantity = 1 }]
        }, "customer-b");

        Assert.Equal("customer-b", claimed!.OwnerId);
        Assert.Equal("New item", Assert.Single(claimed.items).productName);
    }
}
