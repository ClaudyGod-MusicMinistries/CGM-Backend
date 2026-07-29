using ClaudyGod.Domain.Enums;
using ClaudyGod.Domain.Exceptions;
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
    /// <summary>
    /// PostgreSQL xmin concurrency token. EF uses this value to prevent two
    /// reservations from committing against the same event capacity snapshot.
    /// </summary>
    public uint Version { get; private set; }
    public ICollection<TicketReservation> Reservations { get; private set; } = [];

    protected Event() { }

    public static Event Create(string title, DateTime startDate, int totalCapacity,
        string? description = null, string? venue = null, Address? location = null,
        DateTime? endDate = null, bool isFree = true, decimal? ticketPrice = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Event title is required.");
        if (totalCapacity < 0)
            throw new DomainException("Event capacity cannot be negative.");
        if (endDate.HasValue && endDate < startDate)
            throw new DomainException("Event end date cannot be before its start date.");
        if (!isFree && (!ticketPrice.HasValue || ticketPrice <= 0))
            throw new DomainException("A paid event must have a positive ticket price.");

        return new Event
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
    }

    public bool HasAvailableSeats() => AvailableSeats > 0;
    public void IncrementReserved(int count = 1)
    {
        if (count <= 0)
            throw new DomainException("Reservation quantity must be greater than zero.");
        if (ReservedCount + count > TotalCapacity)
            throw new DomainException("The requested seats exceed this event's remaining capacity.");

        ReservedCount += count;
    }
    public void DecrementReserved(int count = 1) => ReservedCount = Math.Max(0, ReservedCount - count);
    public void Cancel() => Status = EventStatus.Cancelled;
    public void Complete() => Status = EventStatus.Completed;
    public void Postpone() => Status = EventStatus.Postponed;
    public void SetFlyer(string path) => FlyerImagePath = path;

    public void Update(string title, DateTime startDate, int totalCapacity,
        string? description, string? venue, Address? location,
        DateTime? endDate, bool isFree, decimal? ticketPrice)
    {
        if (totalCapacity < ReservedCount)
            throw new DomainException(
                $"Total capacity ({totalCapacity}) cannot be less than the {ReservedCount} seats already reserved.");

        Title = title.Trim();
        Description = description;
        Venue = venue;
        Location = location;
        StartDate = startDate;
        EndDate = endDate;
        TotalCapacity = totalCapacity;
        IsFree = isFree;
        TicketPrice = ticketPrice;
    }
}
