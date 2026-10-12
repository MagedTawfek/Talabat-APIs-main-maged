using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

public class CustomerBasketDto
{
    [Required, MinLength(1), MaxLength(100)]
    public List<BasketItemDto> Items { get; set; } = new();
}
