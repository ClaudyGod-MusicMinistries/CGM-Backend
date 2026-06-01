namespace ClaudyGod.Domain.Events;

public sealed record TicketReservedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid ReservationId,
    Guid MinistryEventId,
    string AttendeeEmail,
    string ConfirmationCode,
    int Quantity) : IDomainEvent
{
    public TicketReservedEvent(Guid reservationId, Guid eventId, string email, string code, int qty)
        : this(Guid.NewGuid(), DateTime.UtcNow, reservationId, eventId, email, code, qty) { }
}
