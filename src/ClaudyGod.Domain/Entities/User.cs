using ClaudyGod.Domain.Enums;

namespace ClaudyGod.Domain.Entities;

public class User : AuditableEntity
{
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.User;
    public bool IsActive { get; private set; } = true;
    public DateTime? LastLoginAt { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiry { get; private set; }
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = [];

    protected User() { }

    public static User Create(string username, string email, string passwordHash,
        UserRole role = UserRole.User) =>
        new()
        {
            Username = username.Trim(),
            Email = email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHash,
            Role = role
        };

    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;
    public void SetPasswordHash(string hash) => PasswordHash = hash;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    public void SetRole(UserRole role) => Role = role;

    public void SetPasswordResetToken(string token, TimeSpan expiry)
    {
        PasswordResetToken = token;
        PasswordResetTokenExpiry = DateTime.UtcNow.Add(expiry);
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetTokenExpiry = null;
    }

    public bool IsPasswordResetTokenValid(string token) =>
        string.Equals(PasswordResetToken, token, StringComparison.Ordinal)
        && PasswordResetTokenExpiry > DateTime.UtcNow;
}
