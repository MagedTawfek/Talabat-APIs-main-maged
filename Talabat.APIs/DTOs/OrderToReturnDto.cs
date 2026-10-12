using Talabat.Core.Models.Order_Aggregate;

namespace Talabat.APIs.DTOs;

public class OrderToReturnDto
{
    public int Id { get; set; }
    public string BuyerEmail { get; set; }
    
    public DateTimeOffset OrderDate { get; set; } 
    
    public string Status { get; set; } 
    
    public Address ShipToAddress { get; set; }
    
    
    public List<OrderItemDto> Items { get; set; }
    
    public decimal Total { get; set; }
    
}
