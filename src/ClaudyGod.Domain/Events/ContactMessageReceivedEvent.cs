namespace ClaudyGod.Domain.Events;

public sealed record ContactMessageReceivedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid MessageId,
    string SenderName,
    string SenderEmail) : IDomainEvent
{
    public ContactMessageReceivedEvent(Guid messageId, string name, string email)
        : this(Guid.NewGuid(), DateTime.UtcNow, messageId, name, email) { }
}
