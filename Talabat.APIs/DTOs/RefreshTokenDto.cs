using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

public class RefreshTokenDto
{
    [Required]
    public string RefreshToken { get; set; }
}
