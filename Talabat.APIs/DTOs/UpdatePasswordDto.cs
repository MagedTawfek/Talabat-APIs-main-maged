using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

public class UpdatePasswordDto
{
    [Required, StringLength(128, MinimumLength = 1)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12)]
    [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z0-9]).+$",
        ErrorMessage = "Password must have an uppercase letter, a lowercase letter, a number, and a special character")]
    public string NewPassword { get; set; } = string.Empty;
}
