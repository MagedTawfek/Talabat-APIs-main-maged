namespace Talabat.APIs.DTOs;

public class OrderDto
{
    public AddressDto ShippingAddress { get; set; } = new();
}
