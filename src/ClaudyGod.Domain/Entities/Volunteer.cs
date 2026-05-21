using ClaudyGod.Domain.Enums;

namespace ClaudyGod.Domain.Entities;

public class Volunteer : AuditableEntity
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public VolunteerRole Role { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public bool IsApproved { get; private set; } = false;
    public DateTime? ApprovedAt { get; private set; }

    protected Volunteer() { }

    public static Volunteer Create(string firstName, string lastName, string email,
        VolunteerRole role, string reason) =>
        new()
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.ToLowerInvariant().Trim(),
            Role = role,
            Reason = reason.Trim()
        };

    public void Approve()
    {
        IsApproved = true;
        ApprovedAt = DateTime.UtcNow;
    }
}
