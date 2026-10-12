using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

public class OrderDto
{
    [Required]
    public string BasketId { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int AddressId { get; set; }
}
