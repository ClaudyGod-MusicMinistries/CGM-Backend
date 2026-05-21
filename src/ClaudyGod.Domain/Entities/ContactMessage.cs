namespace ClaudyGod.Domain.Entities;

public class ContactMessage : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public bool IsRead { get; private set; } = false;
    public DateTime? ReadAt { get; private set; }

    protected ContactMessage() { }

    public static ContactMessage Create(string name, string email, string message) =>
        new()
        {
            Name = name.Trim(),
            Email = email.ToLowerInvariant().Trim(),
            Message = message.Trim()
        };

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}
