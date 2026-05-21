using ClaudyGod.Domain.Enums;
using ClaudyGod.Domain.ValueObjects;

namespace ClaudyGod.Domain.Entities;

public class Event : AuditableEntity
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Venue { get; private set; }
    public Address? Location { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public string? FlyerImagePath { get; private set; }
    public int TotalCapacity { get; private set; }
    public int ReservedCount { get; private set; } = 0;
    public int AvailableSeats => TotalCapacity - ReservedCount;
    public bool IsFree { get; private set; } = true;
    public decimal? TicketPrice { get; private set; }
    public EventStatus Status { get; private set; } = EventStatus.Upcoming;
    public ICollection<TicketReservation> Reservations { get; private set; } = [];

    protected Event() { }

    public static Event Create(string title, DateTime startDate, int totalCapacity,
        string? description = null, string? venue = null, Address? location = null,
        DateTime? endDate = null, bool isFree = true, decimal? ticketPrice = null) =>
        new()
        {
            Title = title.Trim(),
            Description = description,
            Venue = venue,
            Location = location,
            StartDate = startDate,
            EndDate = endDate,
            TotalCapacity = totalCapacity,
            IsFree = isFree,
            TicketPrice = ticketPrice
        };

    public bool HasAvailableSeats() => AvailableSeats > 0;
    public void IncrementReserved(int count = 1) => ReservedCount += count;
    public void DecrementReserved(int count = 1) => ReservedCount = Math.Max(0, ReservedCount - count);
    public void Cancel() => Status = EventStatus.Cancelled;
    public void Complete() => Status = EventStatus.Completed;
    public void Postpone() => Status = EventStatus.Postponed;
    public void SetFlyer(string path) => FlyerImagePath = path;
}
