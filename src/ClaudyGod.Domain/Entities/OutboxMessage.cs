namespace ClaudyGod.Domain.Entities;

public sealed class OutboxMessage : BaseEntity
{
    public string Kind { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public DateTime AvailableAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime? LockedUntil { get; set; }
    public string? LockOwner { get; set; }
}
