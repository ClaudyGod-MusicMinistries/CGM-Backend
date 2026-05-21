namespace ClaudyGod.Domain.Entities;

public class Subscriber : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public string UnsubscribeToken { get; private set; } = string.Empty;
    public DateTime? UnsubscribedAt { get; private set; }

    protected Subscriber() { }

    public static Subscriber Create(string name, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return new Subscriber
        {
            Name = name.Trim(),
            Email = email.ToLowerInvariant().Trim(),
            UnsubscribeToken = Guid.NewGuid().ToString("N")
        };
    }

    public void Unsubscribe()
    {
        IsActive = false;
        UnsubscribedAt = DateTime.UtcNow;
    }

    public void Resubscribe()
    {
        IsActive = true;
        UnsubscribedAt = null;
        UnsubscribeToken = Guid.NewGuid().ToString("N");
    }
}
