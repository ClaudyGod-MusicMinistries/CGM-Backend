using MediatR;

namespace ClaudyGod.Domain.Events;

/// <summary>
/// Marker interface for domain events. All domain events implement this so
/// MediatR can dispatch them via INotificationHandler&lt;TEvent&gt;.
/// </summary>
public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}
