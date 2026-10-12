using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Talabat.APIs.Controllers;
using Talabat.APIs.DTOs;
using Talabat.APIs.Errors;
using Talabat.Core.Models.Order_Aggregate;
using Talabat.Core.Services.Contract;
using Talabat.Repository.Identity;
using IdentityAddress = Talabat.Core.Models.Identity.Address;

namespace Talabat.Tests;

public class OrderFlowTests
{
    [Fact]
    public async Task PostOrderUsesAuthenticatedUsersBasketAndReturnsFailureReason()
    {
        const string customerId = "customer-456";
        const int addressId = 72;
        var orderService = new Mock<IOrderService>();
        orderService.Setup(service => service.CreateOrderAsync(
                "customer@example.com", customerId, customerId,
                It.Is<Address>(address => address.FirstName == "Ada" && address.Street == "1 Main St")))
            .ReturnsAsync(OrderCreationResult.Failure(400, "Your basket is empty."));
        await using var identityDbContext = CreateIdentityDbContext();
        identityDbContext.Set<IdentityAddress>().Add(new IdentityAddress
        {
            Id = addressId,
            AppUserId = customerId,
            FName = "Ada",
            LName = "Lovelace",
            Street = "1 Main St",
            City = "London",
            Country = "UK"
        });
        await identityDbContext.SaveChangesAsync();

        var controller = new OrderController(orderService.Object, Mock.Of<AutoMapper.IMapper>(), identityDbContext)
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
            BasketId = customerId,
            AddressId = addressId
        });

        var badRequest = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.Equal("Your basket is empty.", Assert.IsType<ApiResponse>(badRequest.Value).Message);
        orderService.Verify(service => service.CreateOrderAsync(
            "customer@example.com", customerId, customerId,
            It.Is<Address>(address => address.FirstName == "Ada" && address.Street == "1 Main St")), Times.Once);
    }

    [Fact]
    public async Task PostOrderRejectsAddressOwnedByAnotherUser()
    {
        const int addressId = 72;
        var orderService = new Mock<IOrderService>();
        await using var identityDbContext = CreateIdentityDbContext();
        identityDbContext.Set<IdentityAddress>().Add(new IdentityAddress
        {
            Id = addressId,
            AppUserId = "different-user",
            FName = "Ada",
            LName = "Lovelace",
            Street = "1 Main St",
            City = "London",
            Country = "UK"
        });
        await identityDbContext.SaveChangesAsync();
        var controller = new OrderController(orderService.Object, Mock.Of<AutoMapper.IMapper>(), identityDbContext)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, "customer-456"),
                        new Claim(ClaimTypes.Email, "customer@example.com")
                    ], "test"))
                }
            }
        };

        var result = await controller.CreateOrder(new OrderDto { AddressId = addressId });

        Assert.Equal(404, Assert.IsType<NotFoundObjectResult>(result.Result).StatusCode);
        orderService.Verify(service => service.CreateOrderAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Address>()), Times.Never);
    }

    private static AppIdentityDbContext CreateIdentityDbContext()
    {
        var options = new DbContextOptionsBuilder<AppIdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppIdentityDbContext(options);
    }
}
