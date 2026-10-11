namespace Talabat.APIs.DTOs;

/// <summary>Represents an authorization role.</summary>
public sealed record RoleDto(string Name, IReadOnlyList<string> Permissions);
