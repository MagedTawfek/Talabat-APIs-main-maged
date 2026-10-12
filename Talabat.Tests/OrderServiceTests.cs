using Moq;
using Talabat.Core;
using Talabat.Core.Models;
using Talabat.Core.Models.Order_Aggregate;
using Talabat.Core.Repositories.Contract;
using Talabat.Services;

namespace Talabat.Tests;

public class OrderServiceTests
{
    [Fact]
    public async Task CreateOrder_UsesDatabasePricesAndConsumesAuthenticatedUsersBasket()
    {
        var basketRepository = new Mock<IBasketRepository>();
        basketRepository.Setup(repository => repository.GetBasketAsync("user-1", "user-1"))
            .ReturnsAsync(new CustomerBasket("user-1")
            {
                items = [new BasketItem { Id = 7, Price = 0.01m, Quantity = 2 }]
            });
        basketRepository.Setup(repository => repository.RemoveBasketAsync("user-1", "user-1"))
            .ReturnsAsync(true);

        var products = new Mock<IGenericRepository<Product>>();
        products.Setup(repository => repository.GetAsync(7))
            .ReturnsAsync(new Product { Id = 7, Name = "Coffee", Price = 12.50m, PictureUrl = "coffee.png" });

        var orders = new Mock<IGenericRepository<Order>>();
        Order? savedOrder = null;
        orders.Setup(repository => repository.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(order => savedOrder = order)
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.Repository<Product>()).Returns(products.Object);
        unitOfWork.Setup(unit => unit.Repository<Order>()).Returns(orders.Object);
        unitOfWork.Setup(unit => unit.CompleteAsync()).ReturnsAsync(2);

        var service = new OrderService(basketRepository.Object, unitOfWork.Object);
        var address = new Address("Ada", "Lovelace", "1 Main St", "London", "UK");

        var result = await service.CreateOrderAsync("ada@example.com", "user-1", "user-1", address);

        Assert.True(result.Succeeded);
        Assert.Same(savedOrder, result.Order);
        Assert.Equal("ada@example.com", result.Order!.BuyerEmail);
        Assert.Equal(25m, result.Order.Subtotal);
        Assert.Equal(25m, result.Order.GetTotal());
        Assert.Equal(12.50m, Assert.Single(result.Order.OrderItems).Price);
        basketRepository.Verify(repository => repository.RemoveBasketAsync("user-1", "user-1"), Times.Once);
        unitOfWork.Verify(unit => unit.CompleteAsync(), Times.Once);
    }

    [Theory]
    [InlineData(false, true, 1, false, 400)]
    [InlineData(true, false, 1, false, 400)]
    [InlineData(true, true, 0, true, 500)]
    [InlineData(true, true, 1, true, 200)]
    public async Task CreateOrder_ReportsBasketProductAndPersistenceFailures(
        bool basketExists, bool productExists, int saveResult, bool basketRemoved, int expectedStatus)
    {
        var basketRepository = new Mock<IBasketRepository>();
        basketRepository.Setup(repository => repository.GetBasketAsync("user-1", "user-1"))
            .ReturnsAsync(basketExists
                ? new CustomerBasket("user-1") { items = [new BasketItem { Id = 7, Quantity = 1 }] }
                : null);
        basketRepository.Setup(repository => repository.RemoveBasketAsync("user-1", "user-1"))
            .ReturnsAsync(basketRemoved);

        var products = new Mock<IGenericRepository<Product>>();
        products.Setup(repository => repository.GetAsync(7))
            .ReturnsAsync(productExists ? new Product { Id = 7, Name = "Coffee", Price = 10m } : null);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.Repository<Product>()).Returns(products.Object);
        unitOfWork.Setup(unit => unit.Repository<Order>()).Returns(Mock.Of<IGenericRepository<Order>>());
        unitOfWork.Setup(unit => unit.CompleteAsync()).ReturnsAsync(saveResult);

        var result = await new OrderService(basketRepository.Object, unitOfWork.Object)
            .CreateOrderAsync("buyer@example.com", "user-1", "user-1",
                new Address("Ada", "Lovelace", "1 Main St", "London", "UK"));

        Assert.Equal(expectedStatus == 200, result.Succeeded);
        Assert.Equal(expectedStatus, result.StatusCode);
        if (!basketExists || !productExists)
            unitOfWork.Verify(unit => unit.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_ReturnsConflictWhenBasketWasAlreadyConsumed()
    {
        var basketRepository = new Mock<IBasketRepository>();
        basketRepository.Setup(repository => repository.GetBasketAsync("user-1", "user-1"))
            .ReturnsAsync(new CustomerBasket("user-1")
            {
                items = [new BasketItem { Id = 7, Quantity = 1 }]
            });
        basketRepository.Setup(repository => repository.RemoveBasketAsync("user-1", "user-1"))
            .ReturnsAsync(false);
        var products = new Mock<IGenericRepository<Product>>();
        products.Setup(repository => repository.GetAsync(7))
            .ReturnsAsync(new Product { Id = 7, Name = "Coffee", Price = 10m });
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.Repository<Product>()).Returns(products.Object);
        unitOfWork.Setup(unit => unit.Repository<Order>()).Returns(Mock.Of<IGenericRepository<Order>>());

        var result = await new OrderService(basketRepository.Object, unitOfWork.Object)
            .CreateOrderAsync("buyer@example.com", "user-1", "user-1",
                new Address("Ada", "Lovelace", "1 Main St", "London", "UK"));

        Assert.Equal(409, result.StatusCode);
        Assert.False(result.Succeeded);
        unitOfWork.Verify(unit => unit.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task GetOrderByIdForUser_PassesBuyerEmailIntoRepositorySpecification()
    {
        var orders = new Mock<IGenericRepository<Order>>();
        orders.Setup(repository => repository.GetWithSpecAsync(It.IsAny<Talabat.Core.Specification.ISpecification<Order>>() ) )
            .ReturnsAsync((Order?)null);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.Repository<Order>()).Returns(orders.Object);
        var service = new OrderService(Mock.Of<IBasketRepository>(), unitOfWork.Object);

        var result = await service.GetOrderByIdForUserAsync(42, "buyer@example.com");

        Assert.Null(result);
        orders.Verify(repository => repository.GetWithSpecAsync(
            It.Is<Talabat.Core.Specification.ISpecification<Order>>(spec =>
                spec.Criteria!.ToString()!.Contains("BuyerEmail"))), Times.Once);
    }
}
