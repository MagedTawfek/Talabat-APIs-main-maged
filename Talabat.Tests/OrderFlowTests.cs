using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Talabat.APIs.Controllers;
using Talabat.APIs.DTOs;
using Talabat.Core.Models.Order_Aggregate;
using Talabat.Core.Services.Contract;

namespace Talabat.Tests;

public class OrderFlowTests
{
    [Fact]
    public async Task PostOrderUsesAuthenticatedCustomerBasketWithoutClientSuppliedBasketId()
    {
        const string customerId = "customer-456";
        string? basketIdUsed = null;
        var orderService = new Mock<IOrderService>();
        orderService.Setup(service => service.CreateOrderAsync(
                "customer@example.com", customerId, It.IsAny<string>(), It.IsAny<Address>()))
            .Callback<string, string, string, Address>((_, _, basketId, _) => basketIdUsed = basketId)
            .ReturnsAsync((Order?)null);

        var controller = new OrderController(orderService.Object, Mock.Of<AutoMapper.IMapper>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, customerId),
                        new Claim(ClaimTypes.Email, "customer@example.com"),
                        new Claim(ClaimTypes.Role, "Customer")
                    ], "test"))
                }
            }
        };

        var result = await controller.CreateOrder(new OrderDto
        {
            ShippingAddress = new AddressDto
            {
                FirstName = "Ada",
                LastName = "Lovelace",
                Street = "1 Main St",
                City = "London",
                Country = "UK"
            }
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(customerId, basketIdUsed);
        orderService.Verify(service => service.CreateOrderAsync(
            "customer@example.com", customerId, customerId, It.IsAny<Address>()), Times.Once);
    }
}
