namespace ClaudyGod.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;

    public void Revoke(string? ip = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedByIp = ip;
    }
}
