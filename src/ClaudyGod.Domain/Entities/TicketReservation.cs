using ClaudyGod.Domain.Enums;

namespace ClaudyGod.Domain.Entities;

public class TicketReservation : AuditableEntity
{
    public Guid EventId { get; private set; }
    public Event Event { get; private set; } = null!;
    public string AttendeeFirstName { get; private set; } = string.Empty;
    public string AttendeeLastName { get; private set; } = string.Empty;
    public string AttendeeEmail { get; private set; } = string.Empty;
    public string AttendeePhone { get; private set; } = string.Empty;
    public int Quantity { get; private set; } = 1;
    public string ConfirmationCode { get; private set; } = string.Empty;
    public TicketStatus Status { get; private set; } = TicketStatus.Reserved;
    public DateTime? CheckedInAt { get; private set; }
    public string? CheckedInBy { get; private set; }

    protected TicketReservation() { }

    public static TicketReservation Create(Guid eventId, string firstName, string lastName,
        string email, string phone, int quantity, string confirmationCode) =>
        new()
        {
            EventId = eventId,
            AttendeeFirstName = firstName.Trim(),
            AttendeeLastName = lastName.Trim(),
            AttendeeEmail = email.ToLowerInvariant().Trim(),
            AttendeePhone = phone.Trim(),
            Quantity = quantity,
            ConfirmationCode = confirmationCode
        };

    public void CheckIn(string checkedInBy)
    {
        Status = TicketStatus.CheckedIn;
        CheckedInAt = DateTime.UtcNow;
        CheckedInBy = checkedInBy;
    }

    public void Cancel() => Status = TicketStatus.Cancelled;
    public void MarkNoShow() => Status = TicketStatus.NoShow;
}
