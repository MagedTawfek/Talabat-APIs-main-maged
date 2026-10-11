using Talabat.Core;
using Talabat.Core.Models;
using Talabat.Core.Models.Order_Aggregate;
using Talabat.Core.Repositories.Contract;
using Talabat.Core.Services.Contract;
using Talabat.Core.Specification.OrderSpecifications;

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
    
    public async Task<Order?> CreateOrderAsync(string buyerEmail, string basketOwnerId, string basketId, int deliveryMethodId, Address shippingAddress)
    {
        // get basket from the repo
        var basket = await _basketRepo.GetBasketAsync(basketId, basketOwnerId);
        if (basket?.items is null || basket.items.Count == 0) return null;

        // get items from the product repo
        var orderItems = new List<OrderItem>();
        var productRepository = _unitOfWork.Repository<Product>();
        foreach (var item in basket.items)
        {
            var productItem = await productRepository.GetAsync(item.Id);
            if (productItem is null) return null;

            var productItemOrdered = new ProductItemOrder(productItem.Id, productItem.Name, productItem.PictureUrl);
            var itemOrdered = new OrderItem(productItemOrdered, productItem.Price, item.Quantity);
            orderItems.Add(itemOrdered);
        }
        // calculate subtotal
        var subtotal = orderItems.Sum(item => item.Price * item.Quantity);
        
        // get delivery method
        var deliveryMethod = await _unitOfWork.Repository<DeliveryMethod>().GetAsync(deliveryMethodId);
        if (deliveryMethod is null) return null;
        
        var ordersRepo = _unitOfWork.Repository<Order>();
        
        // create order
        var order = new Order(buyerEmail, shippingAddress, deliveryMethod, orderItems, subtotal);
        
        // save to db

        await ordersRepo.AddAsync(order);
        
        var result = await _unitOfWork.CompleteAsync();
            
        if (result <= 0) return null;
        
        return order;
        
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

    public Task<IReadOnlyList<DeliveryMethod>> GetDeliveryMethodsAsync()
    {
        var deliveryMethodsRepo = _unitOfWork.Repository<DeliveryMethod>();
        var deliveryMethods = deliveryMethodsRepo.GetAllAsync();
        return deliveryMethods;
    }
}
