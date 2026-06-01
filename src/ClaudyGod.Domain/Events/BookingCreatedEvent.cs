namespace ClaudyGod.Domain.Events;

public sealed record BookingCreatedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid BookingId,
    string RequesterEmail,
    string RequesterName,
    string EventType) : IDomainEvent
{
    public BookingCreatedEvent(Guid bookingId, string email, string name, string eventType)
        : this(Guid.NewGuid(), DateTime.UtcNow, bookingId, email, name, eventType) { }
}
