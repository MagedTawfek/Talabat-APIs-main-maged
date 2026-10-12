using System.ComponentModel.DataAnnotations;

namespace Talabat.APIs.DTOs;

/// <summary>Request body for adding one permission to a role.</summary>
public sealed record UpdateRolePermissionDto
{
    [Required]
    [StringLength(128, MinimumLength = 1)]
    [RegularExpression("^[A-Za-z][A-Za-z0-9]*(\\.[A-Za-z][A-Za-z0-9]*)+$")]
    public required string Permission { get; init; }
}
