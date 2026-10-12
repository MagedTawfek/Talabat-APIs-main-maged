using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

public class UpdatePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    public string NewPassword { get; set; } = string.Empty;
}
