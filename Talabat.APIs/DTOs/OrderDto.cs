using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

public class OrderDto
{
    [Required, StringLength(450, MinimumLength = 1)]
    public string BasketId { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int AddressId { get; set; }
}
