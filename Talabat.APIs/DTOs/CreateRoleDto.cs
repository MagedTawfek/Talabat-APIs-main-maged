using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

/// <summary>Request body for creating an authorization role.</summary>
public sealed record CreateRoleDto
{
    [Required]
    [StringLength(256, MinimumLength = 2)]
    [RegularExpression("^[\\p{L}\\p{N}][\\p{L}\\p{N} _.-]*$")]
    public required string Name { get; init; }

    [Required, ValidPermissionList]
    public IReadOnlyList<string> Permissions { get; init; } = [];
}
