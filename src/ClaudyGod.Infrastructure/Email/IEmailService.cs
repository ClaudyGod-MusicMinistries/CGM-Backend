namespace ClaudyGod.Infrastructure.Email;

/// <summary>
/// Email service interface for sending transactional emails
/// Implementations can use SendGrid, AWS SES, SMTP, etc.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send an email with HTML and text content
    /// </summary>
    Task<bool> SendEmailAsync(EmailMessage message, CancellationToken ct = default);

    /// <summary>
    /// Send newsletter welcome email
    /// </summary>
    Task<bool> SendNewsletterWelcomeAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Send event registration confirmation
    /// </summary>
    Task<bool> SendEventRegistrationAsync(string email, string name, string eventTitle, DateTime eventDate, CancellationToken ct = default);

    /// <summary>
    /// Send contact form confirmation
    /// </summary>
    Task<bool> SendContactFormConfirmationAsync(string email, string name, string subject, CancellationToken ct = default);

    /// <summary>
    /// Send volunteer application confirmation
    /// </summary>
    Task<bool> SendVolunteerApplicationAsync(string email, string name, string role, CancellationToken ct = default);
}

/// <summary>
/// Email message model
/// </summary>
public class EmailMessage
{
    public required string To { get; set; }
    public string? From { get; set; }
    public required string Subject { get; set; }
    public required string HtmlContent { get; set; }
    public string? TextContent { get; set; }
    public string? ReplyTo { get; set; }
    public Dictionary<string, string>? Tags { get; set; }
}
