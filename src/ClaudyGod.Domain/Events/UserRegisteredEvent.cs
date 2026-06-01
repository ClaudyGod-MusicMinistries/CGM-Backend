namespace ClaudyGod.Domain.Events;

public sealed record UserRegisteredEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid UserId,
    string Email,
    string Username) : IDomainEvent
{
    public UserRegisteredEvent(Guid userId, string email, string username)
        : this(Guid.NewGuid(), DateTime.UtcNow, userId, email, username) { }
}
