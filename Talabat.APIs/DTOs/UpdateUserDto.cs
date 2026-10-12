using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

public class UpdateUserDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, Phone, StringLength(32, MinimumLength = 7)]
    public string PhoneNumber { get; set; } = string.Empty;
}
