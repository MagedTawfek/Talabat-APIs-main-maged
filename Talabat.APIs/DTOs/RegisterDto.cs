using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

public class RegisterDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    [Required]
    public string DisplayName { get; set; }
    [Required]
    [Phone]
    public string PhoneNumber { get; set; }
    [Required]
    [StringLength(128, MinimumLength = 12)]
    [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z0-9]).+$", ErrorMessage = "Password must have an uppercase letter, a lowercase letter, a number, and a special character")]
    public string Password { get; set; }

    
}
