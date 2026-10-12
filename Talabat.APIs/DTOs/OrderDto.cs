using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

public class OrderDto
{
    [Range(1, int.MaxValue)]
    public int DeliveryMethodId { get; set; }
    [Required]
    public AddressDto ShippingAddress { get; set; } = new();
}
