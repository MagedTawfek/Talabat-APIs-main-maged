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
    public async Task CreateOrder_UsesDatabasePricesAndPersistsBuyerScopedOrder()
    {
        var basketRepository = new Mock<IBasketRepository>();
        basketRepository.Setup(repository => repository.GetBasketAsync("basket-1", "user-1"))
            .ReturnsAsync(new CustomerBasket("basket-1")
            {
                items = [new BasketItem { Id = 7, Price = 0.01m, Quantity = 2 }]
            });

        var products = new Mock<IGenericRepository<Product>>();
        products.Setup(repository => repository.GetAsync(7))
            .ReturnsAsync(new Product { Id = 7, Name = "Coffee", Price = 12.50m, PictureUrl = "coffee.png" });

        var deliveryMethods = new Mock<IGenericRepository<DeliveryMethod>>();
        deliveryMethods.Setup(repository => repository.GetAsync(3))
            .ReturnsAsync(new DeliveryMethod("Fast", "Fast delivery", 5m, "1 day") { Id = 3 });

        var orders = new Mock<IGenericRepository<Order>>();
        Order? savedOrder = null;
        orders.Setup(repository => repository.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(order => savedOrder = order)
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.Repository<Product>()).Returns(products.Object);
        unitOfWork.Setup(unit => unit.Repository<DeliveryMethod>()).Returns(deliveryMethods.Object);
        unitOfWork.Setup(unit => unit.Repository<Order>()).Returns(orders.Object);
        unitOfWork.Setup(unit => unit.CompleteAsync()).ReturnsAsync(1);

        var service = new OrderService(basketRepository.Object, unitOfWork.Object);
        var address = new Address("Ada", "Lovelace", "1 Main St", "London", "UK");

        var order = await service.CreateOrderAsync("ada@example.com", "user-1", "basket-1", 3, address);

        Assert.NotNull(order);
        Assert.Same(savedOrder, order);
        Assert.Equal("ada@example.com", order.BuyerEmail);
        Assert.Equal(25m, order.Subtotal);
        Assert.Equal(30m, order.GetTotal());
        Assert.Equal(12.50m, Assert.Single(order.OrderItems).Price);
        unitOfWork.Verify(unit => unit.CompleteAsync(), Times.Once);
    }

    [Theory]
    [InlineData(false, false, false, 0, false)]
    [InlineData(true, false, true, 1, false)]
    [InlineData(true, true, false, 1, false)]
    [InlineData(true, true, true, 0, false)]
    public async Task CreateOrder_RejectsInvalidBasketProductDeliveryOrSave(
        bool basketExists, bool productExists, bool deliveryExists, int saveResult, bool expectedOrder)
    {
        var basketRepository = new Mock<IBasketRepository>();
        basketRepository.Setup(repository => repository.GetBasketAsync("basket-1", "user-1"))
            .ReturnsAsync(basketExists
                ? new CustomerBasket("basket-1") { items = [new BasketItem { Id = 7, Quantity = 1 }] }
                : null);

        var products = new Mock<IGenericRepository<Product>>();
        products.Setup(repository => repository.GetAsync(7))
            .ReturnsAsync(productExists ? new Product { Id = 7, Name = "Coffee", Price = 10m } : null);
        var deliveryMethods = new Mock<IGenericRepository<DeliveryMethod>>();
        deliveryMethods.Setup(repository => repository.GetAsync(3))
            .ReturnsAsync(deliveryExists ? new DeliveryMethod("Standard", "Standard", 2m, "3 days") : null);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.Repository<Product>()).Returns(products.Object);
        unitOfWork.Setup(unit => unit.Repository<DeliveryMethod>()).Returns(deliveryMethods.Object);
        unitOfWork.Setup(unit => unit.Repository<Order>()).Returns(Mock.Of<IGenericRepository<Order>>());
        unitOfWork.Setup(unit => unit.CompleteAsync()).ReturnsAsync(saveResult);

        var service = new OrderService(basketRepository.Object, unitOfWork.Object);
        var order = await service.CreateOrderAsync("buyer@example.com", "user-1", "basket-1", 3,
            new Address("Ada", "Lovelace", "1 Main St", "London", "UK"));

        Assert.Equal(expectedOrder, order is not null);
        if (!basketExists || !productExists || !deliveryExists)
            unitOfWork.Verify(unit => unit.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_RejectsEmptyBasket()
    {
        var basketRepository = new Mock<IBasketRepository>();
        basketRepository.Setup(repository => repository.GetBasketAsync("empty", "user-1"))
            .ReturnsAsync(new CustomerBasket("empty"));
        var unitOfWork = new Mock<IUnitOfWork>();

        var order = await new OrderService(basketRepository.Object, unitOfWork.Object)
            .CreateOrderAsync("buyer@example.com", "user-1", "empty", 1,
                new Address("Ada", "Lovelace", "1 Main St", "London", "UK"));

        Assert.Null(order);
        unitOfWork.Verify(unit => unit.Repository<Product>(), Times.Never);
    }

    [Fact]
    public async Task GetOrderByIdForUser_PassesBuyerEmailIntoRepositorySpecification()
    {
        var orders = new Mock<IGenericRepository<Order>>();
        orders.Setup(repository => repository.GetWithSpecAsync(It.IsAny<Talabat.Core.Specification.ISpecification<Order>>()))
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
