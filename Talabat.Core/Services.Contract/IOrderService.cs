using Talabat.Core.Models.Order_Aggregate;

namespace Talabat.Core.Services.Contract;

public interface IOrderService
{
    Task<OrderCreationResult> CreateOrderAsync(string buyerEmail, string basketOwnerId, string basketId, Address shippingAddress);
    Task<IReadOnlyList<Order>> GetOrdersForUserAsync(string buyerEmail);
    Task<Order?> GetOrderByIdForUserAsync(int orderId, string buyerEmail);
}

public sealed record OrderCreationResult(Order? Order, int StatusCode, string? ErrorMessage)
{
    public bool Succeeded => Order is not null;

    public static OrderCreationResult Success(Order order) => new(order, 200, null);
    public static OrderCreationResult Failure(int statusCode, string message) => new(null, statusCode, message);
}
