using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Talabat.APIs.Controllers;
using Talabat.APIs.DTOs;
using Talabat.Core.Models;
using Talabat.Core.Repositories.Contract;
using Talabat.Core.Services.Contract;

namespace Talabat.Tests;

public class BasketFlowTests
{
    [Fact]
    public async Task PostBasketDerivesBasketIdAndProductDetailsFromAuthenticatedUserAndCatalog()
    {
        const string customerId = "customer-123";
        var productService = new Mock<IProductService>();
        productService.Setup(service => service.GetProductAsync(7)).ReturnsAsync(new Product
        {
            Id = 7,
            Name = "Tea",
            Price = 42,
            PictureUrl = "tea.png"
        });

        CustomerBasket? saved = null;
        var repository = new Mock<IBasketRepository>();
        repository.Setup(repo => repo.UpdateBasketAsync(It.IsAny<CustomerBasket>(), customerId))
            .Callback<CustomerBasket, string>((basket, _) => saved = basket)
            .ReturnsAsync((CustomerBasket basket, string _) => basket);

        var controller = CreateController(customerId, repository.Object, productService.Object);
        var result = await controller.UpdateBasket(new CustomerBasketDto
        {
            Items = [new BasketItemDto { ProductId = 7, Quantity = 2 }]
        });

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(saved);
        Assert.Equal(customerId, saved.id);
        Assert.Equal(customerId, saved.OwnerId);
        var item = Assert.Single(saved.items);
        Assert.Equal(7, item.Id);
        Assert.Equal("Tea", item.productName);
        Assert.Equal(42, item.Price);
        Assert.Equal("tea.png", item.PictureUrl);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public async Task PostBasketRejectsUnknownProductWithoutSavingBasket()
    {
        var productService = new Mock<IProductService>();
        productService.Setup(service => service.GetProductAsync(999)).ReturnsAsync((Product?)null);
        var repository = new Mock<IBasketRepository>();
        var controller = CreateController("customer-123", repository.Object, productService.Object);

        var result = await controller.UpdateBasket(new CustomerBasketDto
        {
            Items = [new BasketItemDto { ProductId = 999, Quantity = 1 }]
        });

        Assert.IsType<NotFoundObjectResult>(result.Result);
        repository.Verify(repo => repo.UpdateBasketAsync(It.IsAny<CustomerBasket>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task PostBasketRejectsEmptyItemLists()
    {
        var repository = new Mock<IBasketRepository>();
        var controller = CreateController("customer-123", repository.Object, Mock.Of<IProductService>());

        var result = await controller.UpdateBasket(new CustomerBasketDto());

        Assert.IsType<BadRequestObjectResult>(result.Result);
        repository.Verify(repo => repo.UpdateBasketAsync(It.IsAny<CustomerBasket>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void BasketAndOrderRequestsDoNotAskTheClientForGeneratedOrCatalogOwnedValues()
    {
        var basketJson = JsonSerializer.Serialize(new CustomerBasketDto
        {
            Items = [new BasketItemDto { ProductId = 7, Quantity = 2 }]
        });
        var orderJson = JsonSerializer.Serialize(new OrderDto { BasketId = "customer-123", AddressId = 3 });

        Assert.Contains("ProductId", basketJson);
        Assert.Contains("Quantity", basketJson);
        Assert.DoesNotContain("BasketId", basketJson);
        Assert.DoesNotContain("Price", basketJson);
        using var basketDocument = JsonDocument.Parse(basketJson);
        using var orderDocument = JsonDocument.Parse(orderJson);
        Assert.False(basketDocument.RootElement.TryGetProperty("Id", out _));
        Assert.False(basketDocument.RootElement.GetProperty("Items")[0].TryGetProperty("Price", out _));
        Assert.True(orderDocument.RootElement.TryGetProperty("BasketId", out _));
        Assert.True(orderDocument.RootElement.TryGetProperty("AddressId", out _));
        Assert.False(basketDocument.RootElement.TryGetProperty("ShippingPrice", out _));
    }

    [Fact]
    public async Task GetAndDeleteBasketUseTheSignedInCustomerIdWithoutRequestIds()
    {
        const string customerId = "customer-123";
        var repository = new Mock<IBasketRepository>();
        repository.Setup(repo => repo.GetBasketAsync(customerId, customerId)).ReturnsAsync((CustomerBasket?)null);
        repository.Setup(repo => repo.DeleteBasketAsync(customerId, customerId)).ReturnsAsync(true);
        var controller = CreateController(customerId, repository.Object, Mock.Of<IProductService>());

        var getResult = await controller.GetBasket();
        var basket = Assert.IsType<OkObjectResult>(getResult.Result).Value as CustomerBasket;
        Assert.Equal(customerId, basket?.id);
        var deleteResult = await controller.DeleteBasket();

        Assert.IsType<OkResult>(deleteResult.Result);
        repository.Verify(repo => repo.GetBasketAsync(customerId, customerId), Times.Once);
        repository.Verify(repo => repo.DeleteBasketAsync(customerId, customerId), Times.Once);
    }

    private static BasketController CreateController(string customerId, IBasketRepository repository, IProductService productService)
    {
        var controller = new BasketController(repository, productService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, customerId), new Claim(ClaimTypes.Role, "Customer")], "test"))
                }
            }
        };
        return controller;
    }
}
