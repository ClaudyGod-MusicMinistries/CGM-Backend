using ClaudyGod.Domain.Enums;

namespace ClaudyGod.Domain.Entities;

public class PrayerRequest : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string RequestText { get; private set; } = string.Empty;
    public bool IsConfidential { get; private set; } = false;
    public PrayerRequestStatus Status { get; private set; } = PrayerRequestStatus.Received;
    public string? AdminResponse { get; private set; }
    public DateTime? RespondedAt { get; private set; }
    public string? RespondedBy { get; private set; }

    protected PrayerRequest() { }

    public static PrayerRequest Create(string name, string email, string subject,
        string requestText, bool isConfidential = false) =>
        new()
        {
            Name = name.Trim(),
            Email = email.ToLowerInvariant().Trim(),
            Subject = subject.Trim(),
            RequestText = requestText.Trim(),
            IsConfidential = isConfidential
        };

    public void Respond(string response, string respondedBy)
    {
        AdminResponse = response;
        Status = PrayerRequestStatus.Responded;
        RespondedAt = DateTime.UtcNow;
        RespondedBy = respondedBy;
    }

    public void MarkPrayedFor() => Status = PrayerRequestStatus.PrayedFor;
    public void Close() => Status = PrayerRequestStatus.Closed;
    public void UpdateStatus(PrayerRequestStatus status) => Status = status;
}
