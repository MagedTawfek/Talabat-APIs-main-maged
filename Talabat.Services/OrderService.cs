using Talabat.Core;
using Talabat.Core.Models;
using Talabat.Core.Models.Order_Aggregate;
using Talabat.Core.Repositories.Contract;
using Talabat.Core.Services.Contract;
using Talabat.Core.Specification.OrderSpecifications;
using Microsoft.EntityFrameworkCore;

namespace Talabat.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly IBasketRepository _basketRepo;

    public OrderService(IBasketRepository basketRepo,
        IUnitOfWork unitOfWork)

    {
        _unitOfWork = unitOfWork;
        _basketRepo = basketRepo;
    }
    
    public async Task<OrderCreationResult> CreateOrderAsync(string buyerEmail, string basketOwnerId, string basketId, Address shippingAddress)
    {
        var basket = await _basketRepo.GetBasketAsync(basketId, basketOwnerId);
        if (basket?.items is null || basket.items.Count == 0)
            return OrderCreationResult.Failure(400, "Your basket is empty or could not be found.");

        // get items from the product repo
        var orderItems = new List<OrderItem>();
        var productRepository = _unitOfWork.Repository<Product>();
        foreach (var item in basket.items)
        {
            var productItem = await productRepository.GetAsync(item.Id);
            if (productItem is null)
                return OrderCreationResult.Failure(400, $"Product {item.Id} is no longer available.");

            var productItemOrdered = new ProductItemOrder(productItem.Id, productItem.Name, productItem.PictureUrl);
            var itemOrdered = new OrderItem(productItemOrdered, productItem.Price, item.Quantity);
            orderItems.Add(itemOrdered);
        }
        var subtotal = orderItems.Sum(item => item.Price * item.Quantity);
        var ordersRepo = _unitOfWork.Repository<Order>();
        var order = new Order(buyerEmail, shippingAddress, orderItems, subtotal);
        await ordersRepo.AddAsync(order);

        // Remove the basket in the same DbContext SaveChanges transaction as the order.
        // If another request already consumed it, EF's delete concurrency check rolls back
        // this order insert, preventing an accidental duplicate order.
        if (!await _basketRepo.RemoveBasketAsync(basketId, basketOwnerId))
            return OrderCreationResult.Failure(409, "This basket has already been used for an order.");

        try
        {
            var savedChanges = await _unitOfWork.CompleteAsync();
            if (savedChanges <= 0)
                return OrderCreationResult.Failure(500, "The order could not be saved.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return OrderCreationResult.Failure(409, "This basket has already been used for an order.");
        }

        return OrderCreationResult.Success(order);
    }

    public async Task<IReadOnlyList<Order>> GetOrdersForUserAsync(string buyerEmail)
    {
        var ordersRepo = _unitOfWork.Repository<Order>();
        var spec = new OrderSpecifications(buyerEmail);
        var orders = await ordersRepo.GetAllWithSpecAsync(spec);
        return orders.ToList(); 
    }


    public Task<Order?> GetOrderByIdForUserAsync(int orderId, string buyerEmail)
    {
        var ordersRepo = _unitOfWork.Repository<Order>();
        var spec = new OrderSpecifications(orderId,buyerEmail);
        var order = ordersRepo.GetWithSpecAsync(spec);
        return order;
    }

}
