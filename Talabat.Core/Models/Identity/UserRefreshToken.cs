namespace Talabat.Core.Models.Identity;

public sealed class UserRefreshToken
{
    public long Id { get; set; }
    public string UserId { get; set; } = null!;
    public AppUser User { get; set; } = null!;
    public string TokenHash { get; set; } = null!;
    public string FamilyId { get; set; } = null!;
    public string UserSecurityStamp { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
