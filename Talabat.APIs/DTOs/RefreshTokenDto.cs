using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

public class RefreshTokenDto
{
    [Required, StringLength(256, MinimumLength = 32)]
    public string RefreshToken { get; set; } = string.Empty;
}
