using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

public class LoginDto
{
    [Required]
    [EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [StringLength(128)]
    public string Password { get; set; } = string.Empty;
}
