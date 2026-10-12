using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using Talabat.Core.Models;
using Talabat.Core.Models.Identity;
using Talabat.Core.Models.Order_Aggregate;
using Talabat.Repository.Data;
using Talabat.Repository.Identity;
using IdentityAddress = Talabat.Core.Models.Identity.Address;
using OrderAddress = Talabat.Core.Models.Order_Aggregate.Address;

namespace Talabat.Tests;

public class DatabaseSchemaTests
{
    [Fact]
    public void StoreDatabase_MapsExpectedTablesAndPrimaryKeys()
    {
        using var context = CreateStoreContext();

        Assert.Equal(
            new[] { "BasketItems", "CustomerBaskets", "OrderItems", "Orders", "Products" },
            GetTableNames(context.Model));
        Assert.Equal(new[] { "Id" }, GetPrimaryKeyProperties(context.Model, typeof(Product)));
        Assert.Equal(new[] { "id" }, GetPrimaryKeyProperties(context.Model, typeof(CustomerBasket)));
        Assert.Equal(new[] { "BasketId", "Id" }, GetPrimaryKeyProperties(context.Model, typeof(BasketItem)));
        Assert.Equal(new[] { "Id" }, GetPrimaryKeyProperties(context.Model, typeof(Order)));
        Assert.Equal(new[] { "Id" }, GetPrimaryKeyProperties(context.Model, typeof(OrderItem)));

        AssertModelMatchesLatestMigration(context);
    }

    [Fact]
    public void StoreDatabase_MapsBasketAndOrderRelationshipsAndOwnedData()
    {
        using var context = CreateStoreContext();
        var model = context.Model;

        var basket = GetEntityType(model, typeof(CustomerBasket));
        var basketItem = GetEntityType(model, typeof(BasketItem));
        var basketForeignKey = Assert.Single(basketItem.GetForeignKeys());
        Assert.Equal(basket, basketForeignKey.PrincipalEntityType);
        Assert.Equal(new[] { "BasketId" }, basketForeignKey.Properties.Select(property => property.Name));
        Assert.Equal(DeleteBehavior.Cascade, basketForeignKey.DeleteBehavior);

        var order = GetEntityType(model, typeof(Order));
        var orderItem = GetEntityType(model, typeof(OrderItem));
        var orderItemForeignKey = Assert.Single(orderItem.GetForeignKeys());
        Assert.Equal(order, orderItemForeignKey.PrincipalEntityType);
        Assert.Equal(new[] { "OrderId" }, orderItemForeignKey.Properties.Select(property => property.Name));
        Assert.False(orderItemForeignKey.IsRequired);

        var orderAddress = GetEntityType(model, typeof(OrderAddress));
        var orderedProduct = GetEntityType(model, typeof(ProductItemOrder));
        Assert.True(orderAddress.IsOwned());
        Assert.Equal("Orders", orderAddress.GetTableName());
        Assert.True(orderedProduct.IsOwned());
        Assert.Equal("OrderItems", orderedProduct.GetTableName());
    }

    [Fact]
    public void IdentityDatabase_MapsExpectedTablesAndPrimaryKeys()
    {
        using var context = CreateIdentityContext();

        Assert.Equal(
            new[]
            {
                "Addresses",
                "AspNetRoleClaims",
                "AspNetRoles",
                "AspNetUserClaims",
                "AspNetUserLogins",
                "AspNetUserRoles",
                "AspNetUserTokens",
                "AspNetUsers",
                "UserRefreshTokens"
            },
            GetTableNames(context.Model));
        Assert.Equal(new[] { "Id" }, GetPrimaryKeyProperties(context.Model, typeof(AppUser)));
        Assert.Equal(new[] { "Id" }, GetPrimaryKeyProperties(context.Model, typeof(IdentityRole)));
        Assert.Equal(new[] { "Id" }, GetPrimaryKeyProperties(context.Model, typeof(IdentityAddress)));
        Assert.Equal(new[] { "Id" }, GetPrimaryKeyProperties(context.Model, typeof(UserRefreshToken)));
        Assert.Equal(
            new[] { "UserId", "RoleId" },
            GetPrimaryKeyProperties(context.Model, typeof(IdentityUserRole<string>)));

        AssertModelMatchesLatestMigration(context);
    }

    [Fact]
    public void IdentityDatabase_MapsUserAddressAndRefreshTokenRelationships()
    {
        using var context = CreateIdentityContext();
        var model = context.Model;
        var user = GetEntityType(model, typeof(AppUser));

        var address = GetEntityType(model, typeof(IdentityAddress));
        var addressForeignKey = Assert.Single(address.GetForeignKeys());
        Assert.Equal(user, addressForeignKey.PrincipalEntityType);
        Assert.True(addressForeignKey.IsUnique);
        Assert.Equal(new[] { "AppUserId" }, addressForeignKey.Properties.Select(property => property.Name));

        var refreshToken = GetEntityType(model, typeof(UserRefreshToken));
        var refreshTokenForeignKey = Assert.Single(refreshToken.GetForeignKeys());
        Assert.Equal(user, refreshTokenForeignKey.PrincipalEntityType);
        Assert.Equal(new[] { "UserId" }, refreshTokenForeignKey.Properties.Select(property => property.Name));
        Assert.Equal(DeleteBehavior.Cascade, refreshTokenForeignKey.DeleteBehavior);
        Assert.Contains(
            refreshToken.GetIndexes(),
            index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual(["TokenHash"]));
    }

    private static StoreContext CreateStoreContext()
    {
        var options = new DbContextOptionsBuilder<StoreContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=TalabatStoreSchemaTests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new StoreContext(options);
    }

    private static AppIdentityDbContext CreateIdentityContext()
    {
        var options = new DbContextOptionsBuilder<AppIdentityDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=TalabatIdentitySchemaTests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new AppIdentityDbContext(options);
    }

    private static string[] GetTableNames(IModel model) => model.GetEntityTypes()
        .Select(entityType => entityType.GetTableName())
        .Where(tableName => tableName is not null)
        .Select(tableName => tableName!)
        .Distinct()
        .OrderBy(tableName => tableName, StringComparer.Ordinal)
        .ToArray();

    private static string[] GetPrimaryKeyProperties(IModel model, Type entityType)
    {
        var primaryKey = GetEntityType(model, entityType).FindPrimaryKey();
        Assert.NotNull(primaryKey);
        return primaryKey.Properties.Select(property => property.Name).ToArray();
    }

    private static IEntityType GetEntityType(IModel model, Type entityType)
    {
        var metadata = model.FindEntityType(entityType);
        Assert.NotNull(metadata);
        return metadata;
    }

    private static void AssertModelMatchesLatestMigration(DbContext context)
    {
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();
        var snapshot = Assert.IsAssignableFrom<ModelSnapshot>(migrationsAssembly.ModelSnapshot);
        var snapshotSchema = GetSchemaSignature(snapshot.Model);
        var contextSchema = GetSchemaSignature(context.Model);
        Assert.True(
            snapshotSchema.SequenceEqual(contextSchema),
            $"Migration snapshot:{Environment.NewLine}{string.Join(Environment.NewLine, snapshotSchema)}" +
            $"{Environment.NewLine}Current model:{Environment.NewLine}{string.Join(Environment.NewLine, contextSchema)}");
    }

    private static string[] GetSchemaSignature(IModel model) => model.GetEntityTypes()
        .OrderBy(GetSchemaEntityName, StringComparer.Ordinal)
        .Select(entityType =>
        {
            var properties = string.Join(
                ",",
                entityType.GetProperties()
                    .OrderBy(property => property.Name, StringComparer.Ordinal)
                    .Select(property => $"{property.Name}:{property.GetColumnType()}:{property.IsNullable}"));
            var primaryKey = string.Join(
                ",",
                entityType.FindPrimaryKey()?.Properties.Select(property => property.Name) ?? []);
            var foreignKeys = string.Join(
                ";",
                entityType.GetForeignKeys()
                    .Select(foreignKey =>
                        $"{string.Join(",", foreignKey.Properties.Select(property => property.Name))}" +
                        $"->{foreignKey.PrincipalEntityType.Name}" +
                        $"({string.Join(",", foreignKey.PrincipalKey.Properties.Select(property => property.Name))})" +
                        $":{foreignKey.IsRequired}:{foreignKey.IsUnique}:{foreignKey.DeleteBehavior}")
                    .OrderBy(signature => signature, StringComparer.Ordinal));

            return $"{GetSchemaEntityName(entityType)}|{entityType.GetTableName()}|{primaryKey}|{properties}|{foreignKeys}";
        })
        .ToArray();

    private static string GetSchemaEntityName(IEntityType entityType)
    {
        if (!entityType.IsOwned())
        {
            return entityType.Name;
        }

        var ownership = entityType.FindOwnership()!;
        var navigationName = ownership.PrincipalToDependent?.Name ?? ownership.DependentToPrincipal?.Name;
        return $"{ownership.PrincipalEntityType.Name}.{navigationName}";
    }
}
