using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

/// <summary>Request body for assigning one role to a user account.</summary>
public sealed record AssignRoleDto
{
    [Required]
    [StringLength(256, MinimumLength = 2)]
    [RegularExpression("^[\\p{L}\\p{N}][\\p{L}\\p{N} _.-]*$")]
    public required string RoleName { get; init; }
}
