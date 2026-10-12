using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Talabat.APIs.Controllers;
using Talabat.APIs.DTOs;

namespace Talabat.Tests;

public class EndpointContractTests
{
    [Fact]
    public void EveryControllerActionIsBoundToAnHttpVerbAndRoute()
    {
        var controllers = new[]
        {
            typeof(AccountController), typeof(BasketController), typeof(ErrorsController),
            typeof(OrderController), typeof(ProductsController), typeof(RolesController)
        };

        foreach (var controller in controllers)
        {
            var actions = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttributes().OfType<HttpMethodAttribute>().Any() ||
                                 method.IsDefined(typeof(AcceptVerbsAttribute), inherit: true))
                .ToArray();

            Assert.True(actions.Length > 0, $"No HTTP actions found for {controller.Name}");
            var hasControllerRoute = controller.GetCustomAttributes<RouteAttribute>(inherit: true).Any() ||
                                     typeof(BaseApiController).GetCustomAttributes<RouteAttribute>(inherit: true).Any();
            Assert.True(hasControllerRoute, $"No route found for {controller.Name}");
            Assert.All(actions, action => Assert.True(
                action.GetCustomAttributes().OfType<HttpMethodAttribute>().Any() ||
                action.IsDefined(typeof(AcceptVerbsAttribute), inherit: true)));
        }

        Assert.Equal(26, controllers.Sum(controller => controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Count(method => method.GetCustomAttributes().OfType<HttpMethodAttribute>().Any() ||
                             method.IsDefined(typeof(AcceptVerbsAttribute), inherit: true))));
    }

    [Fact]
    public void ClientRequestsOnlyContainValuesTheClientOwns()
    {
        Assert.DoesNotContain(typeof(CreateProductDto).GetProperties(), property => property.Name.Contains("Brand", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(ProductToReturnDto).GetProperties(), property => property.Name.Contains("Brand", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(CreateProductDto).GetProperties(), property => property.Name.Contains("Category", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(ProductToReturnDto).GetProperties(), property => property.Name.Contains("Category", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(CustomerBasketDto).GetProperties(), property => property.Name is "Id" or "OwnerId" or "ShippingPrice");
        Assert.Equal(new[] { "ProductId", "Quantity" }, typeof(BasketItemDto).GetProperties().Select(property => property.Name).OrderBy(name => name));
        Assert.Equal(new[] { "AddressId", "BasketId" }, typeof(OrderDto).GetProperties().Select(property => property.Name).OrderBy(name => name));
        Assert.Contains(typeof(AddressDto).GetProperties(), property => property.Name == "Id");
        Assert.DoesNotContain(typeof(AddressDto).GetProperties(), property => property.Name == "AppUserId");
    }
}
