namespace ClaudyGod.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string UserId { get; set; } = "anonymous";
    public string UserEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Succeeded { get; set; } = true;
    public string? FailureReason { get; set; }
}
