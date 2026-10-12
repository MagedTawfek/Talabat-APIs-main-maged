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
    public void ProductCatalogIsPublicAndProductCreationRequiresItsPermission()
    {
        var actions = HttpActions(typeof(ProductsController)).ToArray();
        Assert.All(actions.Where(method => method.Name != nameof(ProductsController.CreateProduct)),
            method => Assert.True(method.IsDefined(typeof(AllowAnonymousAttribute), inherit: true)));

        var createProduct = typeof(ProductsController).GetMethod(nameof(ProductsController.CreateProduct))!;
        Assert.Equal("permission:products.create", Assert.IsType<AuthorizeAttribute>(Attribute.GetCustomAttribute(
            createProduct, typeof(AuthorizeAttribute), inherit: true)).Policy);
    }

    [Fact]
    public void BasketAndOrderActionsRequireThePermissionForThatAction()
    {
        AssertPolicy<BasketController>(nameof(BasketController.GetBasket), "permission:basket.read");
        AssertPolicy<BasketController>(nameof(BasketController.UpdateBasket), "permission:basket.write");
        AssertPolicy<BasketController>(nameof(BasketController.DeleteBasket), "permission:basket.delete");
        AssertPolicy<OrderController>(nameof(OrderController.CreateOrder), "permission:orders.create");
        AssertPolicy<OrderController>(nameof(OrderController.GetOrdersForUser), "permission:orders.read");
        AssertPolicy<OrderController>(nameof(OrderController.GetOrderById), "permission:orders.read");

        Assert.Null(typeof(OrderController).GetMethod("GetDeliveryMethods"));
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
    public void RoleManagementRequiresPermissionAndCredentialEndpointsAreRateLimited()
    {
        Assert.Equal("permission:roles.manage", Assert.IsType<AuthorizeAttribute>(Attribute.GetCustomAttribute(
            typeof(RolesController), typeof(AuthorizeAttribute), inherit: true)).Policy);

        foreach (var action in new[] { nameof(AccountController.Login), nameof(AccountController.Register), nameof(AccountController.RefreshToken) })
        {
            var method = typeof(AccountController).GetMethod(action)!;
            Assert.Contains(method.GetCustomAttributes<EnableRateLimitingAttribute>(), policy => policy.PolicyName == "auth");
        }
    }

    [Fact]
    public void EmailExistenceEndpointRequiresPermission()
    {
        var action = typeof(AccountController).GetMethod(nameof(AccountController.CheckEmailExistsAsync))!;
        Assert.Equal("permission:accounts.email.lookup", Assert.IsType<AuthorizeAttribute>(Attribute.GetCustomAttribute(
            action, typeof(AuthorizeAttribute), inherit: true)).Policy);
    }

    private static void AssertPolicy<TController>(string actionName, string expectedPolicy)
    {
        var action = typeof(TController).GetMethod(actionName)!;
        Assert.Equal(expectedPolicy, Assert.IsType<AuthorizeAttribute>(Attribute.GetCustomAttribute(
            action, typeof(AuthorizeAttribute), inherit: true)).Policy);
    }

    private static IEnumerable<MethodInfo> HttpActions(Type controller) => controller
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
        .Where(method => method.GetCustomAttributes().OfType<HttpMethodAttribute>().Any());
}
