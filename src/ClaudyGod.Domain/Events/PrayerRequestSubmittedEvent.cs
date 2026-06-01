namespace ClaudyGod.Domain.Events;

public sealed record PrayerRequestSubmittedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid RequestId,
    string SubmitterEmail,
    string Subject,
    bool IsConfidential) : IDomainEvent
{
    public PrayerRequestSubmittedEvent(Guid requestId, string email, string subject, bool isConfidential)
        : this(Guid.NewGuid(), DateTime.UtcNow, requestId, email, subject, isConfidential) { }
}
