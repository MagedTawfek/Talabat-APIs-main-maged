using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Talabat.APIs.Controllers;

namespace Talabat.Tests;

public class AuthorizationPolicyTests
{
    [Fact]
    public void ProductCatalogIsPublicReadOnlySurface()
    {
        Assert.True(typeof(ProductsController).IsDefined(typeof(AllowAnonymousAttribute), inherit: true));
        Assert.All(HttpActions(typeof(ProductsController)), method => Assert.False(method.IsDefined(typeof(AuthorizeAttribute), true)));
    }

    [Fact]
    public void BasketAndOrderActionsAreCustomerOnlyExceptDeliveryCatalog()
    {
        Assert.Equal("Customer", Assert.IsType<AuthorizeAttribute>(Attribute.GetCustomAttribute(
            typeof(BasketController), typeof(AuthorizeAttribute), inherit: true)).Roles);
        Assert.Equal("Customer", Assert.IsType<AuthorizeAttribute>(Attribute.GetCustomAttribute(
            typeof(OrderController), typeof(AuthorizeAttribute), inherit: true)).Roles);

        var deliveryMethods = typeof(OrderController).GetMethod(nameof(OrderController.GetDeliveryMethods))!;
        Assert.True(deliveryMethods.IsDefined(typeof(AllowAnonymousAttribute), inherit: true));
    }

    [Fact]
    public void OnlyLoginRegistrationAndRefreshAreAnonymousAccountActions()
    {
        Assert.True(typeof(AccountController).IsDefined(typeof(AuthorizeAttribute), inherit: true));
        var anonymousActions = HttpActions(typeof(AccountController))
            .Where(method => method.IsDefined(typeof(AllowAnonymousAttribute), inherit: true))
            .Select(method => method.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(new[] { nameof(AccountController.Login), nameof(AccountController.RefreshToken), nameof(AccountController.Register) }, anonymousActions);
        Assert.All(HttpActions(typeof(AccountController)).Where(method => !anonymousActions.Contains(method.Name)),
            method => Assert.False(method.IsDefined(typeof(AllowAnonymousAttribute), inherit: true)));
    }

    [Fact]
    public void RoleManagementIsAdminOnlyAndCredentialEndpointsAreRateLimited()
    {
        Assert.Equal("Admin", Assert.IsType<AuthorizeAttribute>(Attribute.GetCustomAttribute(
            typeof(RolesController), typeof(AuthorizeAttribute), inherit: true)).Roles);

        foreach (var action in new[] { nameof(AccountController.Login), nameof(AccountController.Register), nameof(AccountController.RefreshToken) })
        {
            var method = typeof(AccountController).GetMethod(action)!;
            Assert.Contains(method.GetCustomAttributes<EnableRateLimitingAttribute>(), policy => policy.PolicyName == "auth");
        }
    }

    [Fact]
    public void EmailExistenceEndpointIsAdminOnly()
    {
        var action = typeof(AccountController).GetMethod(nameof(AccountController.CheckEmailExistsAsync))!;
        Assert.Equal("Admin", Assert.IsType<AuthorizeAttribute>(Attribute.GetCustomAttribute(
            action, typeof(AuthorizeAttribute), inherit: true)).Roles);
    }

    private static IEnumerable<MethodInfo> HttpActions(Type controller) => controller
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
        .Where(method => method.GetCustomAttributes().OfType<HttpMethodAttribute>().Any());
}
