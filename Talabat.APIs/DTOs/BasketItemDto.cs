using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

public class BasketItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "ProductId must be a positive product ID")]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
}
