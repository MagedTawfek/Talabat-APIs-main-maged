using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

public class CreateProductDto
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string Description { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string PictureUrl { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Price { get; set; }

}
