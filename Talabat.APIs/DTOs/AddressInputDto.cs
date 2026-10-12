using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

public class AddressInputDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(250, MinimumLength = 3)]
    public string Street { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string Country { get; set; } = string.Empty;
}
