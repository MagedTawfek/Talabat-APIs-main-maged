using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

public class RegisterDto
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, Phone, StringLength(32, MinimumLength = 7)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12)]
    [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z0-9]).+$",
        ErrorMessage = "Password must have an uppercase letter, a lowercase letter, a number, and a special character")]
    public string Password { get; set; } = string.Empty;
}
