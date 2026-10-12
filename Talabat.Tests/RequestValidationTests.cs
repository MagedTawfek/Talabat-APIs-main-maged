using System.ComponentModel.DataAnnotations;
using Talabat.APIs.DTOs;
using Talabat.Core.Specifications.ProductSpecifications;

namespace Talabat.Tests;

public class RequestValidationTests
{
    [Theory]
    [InlineData("StrongPass123!", true)]
    [InlineData("short", false)]
    [InlineData("lowercase1!", false)]
    [InlineData("Uppercase!", false)]
    [InlineData("NoSpecial123", false)]
    [InlineData("Pass123!", false)]
    [InlineData("Password12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890!", false)]
    public void RegistrationPassword_MatchesDocumentedStrengthRules(string password, bool isValid)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(new RegisterDto
        {
            Email = "ada@example.com",
            DisplayName = "Ada",
            PhoneNumber = "+44123456789",
            Password = password
        });

        var result = Validator.TryValidateObject(context.ObjectInstance, context, results, validateAllProperties: true);

        Assert.Equal(isValid, result);
    }

    [Fact]
    public void BasketItemAndOrderRejectInvalidClientOwnedValues()
    {
        Assert.False(IsValid(new BasketItemDto { ProductId = 0, Quantity = 0 }));
        Assert.False(IsValid(new BasketItemDto { ProductId = 4, Quantity = 0 }));
        Assert.False(IsValid(new OrderDto { AddressId = 0, BasketId = "user-1" }));
        Assert.False(IsValid(new OrderDto { AddressId = 12 }));
        Assert.True(IsValid(new OrderDto { AddressId = 12, BasketId = "user-1" }));
        Assert.True(IsValid(new BasketItemDto { ProductId = 4, Quantity = 2 }));
    }

    [Fact]
    public void ProductInputRejectsInvalidLengthsAndPrices()
    {
        Assert.False(IsValid(new CreateProductDto
        {
            Name = new string('x', 101), Description = "A valid description", PictureUrl = "/product.png", Price = 1m
        }));
        Assert.False(IsValid(new CreateProductDto
        {
            Name = "Tea", Description = "short", PictureUrl = "/product.png", Price = 0m
        }));
        Assert.False(IsValid(new CreateProductDto
        {
            Name = "Tea", Description = "A valid description", PictureUrl = "/product.png", Price = 1.256m
        }));
        Assert.True(IsValid(new CreateProductDto
        {
            Name = "Tea", Description = "A valid description", PictureUrl = "/product.png", Price = 1.25m
        }));
    }

    [Fact]
    public void AddressAndProfileInputsRejectMissingOrOverlongFields()
    {
        Assert.False(IsValid(new AddressInputDto { FirstName = "", LastName = "Lovelace", Street = "1 Main St", City = "London", Country = "UK" }));
        Assert.False(IsValid(new AddressInputDto { FirstName = "Ada", LastName = "Lovelace", Street = new string('s', 251), City = "London", Country = "UK" }));
        Assert.True(IsValid(new AddressInputDto { FirstName = "Ada", LastName = "Lovelace", Street = "1 Main St", City = "London", Country = "UK" }));
        Assert.False(IsValid(new UpdateUserDto { DisplayName = "A", PhoneNumber = "bad" }));
        Assert.True(IsValid(new UpdateUserDto { DisplayName = "Ada Lovelace", PhoneNumber = "+44123456789" }));
    }

    [Fact]
    public void RoleAndPermissionInputsRejectMalformedValues()
    {
        Assert.False(IsValid(new CreateRoleDto { Name = "!invalid", Permissions = [] }));
        Assert.True(IsValid(new CreateRoleDto { Name = "Support", Permissions = ["orders.read", "basket.write"] }));
        Assert.False(IsValid(new CreateRoleDto { Name = "Support", Permissions = [""] }));
        Assert.False(IsValid(new UpdateRolePermissionDto { Permission = "orders read" }));
        Assert.True(IsValid(new UpdateRolePermissionDto { Permission = "orders.read" }));
        Assert.False(IsValid(new AssignRoleDto { RoleName = "?" }));
    }

    [Fact]
    public void ProductQueryRejectsInvalidPaginationAndSortValues()
    {
        Assert.False(IsValid(new ProductsSpecParams { PageIndex = 0 }));
        Assert.False(IsValid(new ProductsSpecParams { PageSize = 51 }));
        Assert.False(IsValid(new ProductsSpecParams { sort = "price;drop" }));
        Assert.True(IsValid(new ProductsSpecParams { PageIndex = 1, PageSize = 25, sort = "priceAsc", Search = "tea" }));
    }

    [Fact]
    public void BasketAndCredentialInputsRejectOversizedOrMalformedValues()
    {
        Assert.False(IsValid(new CustomerBasketDto { Items = [] }));
        Assert.True(IsValid(new CustomerBasketDto { Items = [new BasketItemDto { ProductId = 4, Quantity = 1 }] }));
        Assert.False(IsValid(new LoginDto { Email = "not-an-email", Password = "Password123!" }));
        Assert.False(IsValid(new UpdatePasswordDto { CurrentPassword = "CurrentPass123!", NewPassword = "weak" }));
        Assert.True(IsValid(new UpdatePasswordDto { CurrentPassword = "CurrentPass123!", NewPassword = "NewPassword456!" }));
        Assert.False(IsValid(new RefreshTokenDto { RefreshToken = "short" }));
    }

    private static bool IsValid(object value)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(value, new ValidationContext(value), results, validateAllProperties: true);
    }
}
