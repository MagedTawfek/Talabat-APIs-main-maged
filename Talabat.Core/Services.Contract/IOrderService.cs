using Talabat.Core.Models.Order_Aggregate;

namespace Talabat.Core.Services.Contract;

public interface IOrderService
{
    Task<Order?> CreateOrderAsync(string buyerEmail, string basketOwnerId, string basketId, Address shippingAddress);
    Task<IReadOnlyList<Order>> GetOrdersForUserAsync(string buyerEmail);
    Task<Order?> GetOrderByIdForUserAsync(int orderId, string buyerEmail);
}
