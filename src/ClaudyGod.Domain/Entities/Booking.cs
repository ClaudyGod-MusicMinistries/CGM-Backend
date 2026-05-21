using ClaudyGod.Domain.Enums;
using ClaudyGod.Domain.ValueObjects;

namespace ClaudyGod.Domain.Entities;

public class Booking : AuditableEntity
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public CountryCode CountryCode { get; private set; }
    public string Organization { get; private set; } = string.Empty;
    public string OrgType { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string EventDetails { get; private set; } = string.Empty;
    public DateTime EventDate { get; private set; }
    public Address Address { get; private set; } = null!;
    public bool AgreeTerms { get; private set; }
    public BookingStatus Status { get; private set; } = BookingStatus.Pending;
    public string? AdminNotes { get; private set; }

    protected Booking() { }

    public static Booking Create(
        string firstName, string lastName, string email, string phone,
        CountryCode countryCode, string organization, string orgType,
        string eventType, string eventDetails, DateTime eventDate,
        Address address)
    {
        return new Booking
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.ToLowerInvariant().Trim(),
            Phone = phone.Trim(),
            CountryCode = countryCode,
            Organization = organization.Trim(),
            OrgType = orgType.Trim(),
            EventType = eventType.Trim(),
            EventDetails = eventDetails.Trim(),
            EventDate = eventDate,
            Address = address,
            AgreeTerms = true
        };
    }

    public void UpdateStatus(BookingStatus status, string? notes = null)
    {
        Status = status;
        if (notes is not null) AdminNotes = notes;
    }
}
