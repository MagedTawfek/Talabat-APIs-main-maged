using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

public class OrderDto
{
    [Required]
    public string BasketId { get; set; } = string.Empty;

    public AddressDto ShippingAddress { get; set; } = new();
}
