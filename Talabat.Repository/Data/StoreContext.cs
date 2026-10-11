using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Talabat.Core.Models;
using Talabat.Core.Models.Order_Aggregate;

namespace Talabat.Repository.Data;

public class StoreContext : DbContext
{
    public StoreContext(DbContextOptions<StoreContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.Entity<CustomerBasket>(basket =>
        {
            basket.HasKey(entity => entity.id);
            basket.Property(entity => entity.id).HasMaxLength(450);
            basket.Property(entity => entity.OwnerId).HasMaxLength(450);
            basket.HasMany(entity => entity.items)
                .WithOne()
                .HasForeignKey("BasketId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BasketItem>(item =>
        {
            item.HasKey("BasketId", nameof(BasketItem.Id));
            item.Property<string>("BasketId").HasMaxLength(450);
        });
        
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<ProductBrand> ProductBrands { get; set; }
    public DbSet<ProductCategory>  ProductCategories { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<DeliveryMethod> DeliveryMethods { get; set; }
    public DbSet<CustomerBasket> CustomerBaskets { get; set; }
    public DbSet<BasketItem> BasketItems { get; set; }
    
    
    
    
}
