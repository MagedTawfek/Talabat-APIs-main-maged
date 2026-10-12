using System.ComponentModel.DataAnnotations;
using Talabat.APIs.DTOs;

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
        Assert.False(IsValid(new OrderDto { DeliveryMethodId = 0, ShippingAddress = new AddressDto() }));
        Assert.True(IsValid(new BasketItemDto { ProductId = 4, Quantity = 2 }));
    }

    private static bool IsValid(object value)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(value, new ValidationContext(value), results, validateAllProperties: true);
    }
}
