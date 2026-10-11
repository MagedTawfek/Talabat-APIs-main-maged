using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

/// <summary>Request body for adding one permission to a role.</summary>
public sealed record UpdateRolePermissionDto
{
    [Required]
    [StringLength(128, MinimumLength = 1)]
    public required string Permission { get; init; }
}
