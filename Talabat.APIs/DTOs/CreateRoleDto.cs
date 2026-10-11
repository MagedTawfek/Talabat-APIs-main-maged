using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

/// <summary>Request body for creating an authorization role.</summary>
public sealed record CreateRoleDto
{
    [Required]
    [StringLength(256, MinimumLength = 2)]
    public required string Name { get; init; }

    public IReadOnlyList<string> Permissions { get; init; } = [];
}
