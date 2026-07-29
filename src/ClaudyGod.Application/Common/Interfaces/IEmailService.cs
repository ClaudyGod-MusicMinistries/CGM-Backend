using Microsoft.Extensions.Logging;

namespace ClaudyGod.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);

    Task SendFromTemplateAsync(string to, string templateName,
        Dictionary<string, string> variables, CancellationToken ct = default);

    Task SendWithAttachmentAsync(string to, string subject, string htmlBody,
        Stream attachment, string attachmentName, string attachmentMimeType,
        CancellationToken ct = default);
}

/// <summary>
/// Compatibility helpers for enqueueing durable email work. Enqueue failures are
/// intentionally propagated so the business record and notification cannot diverge.
/// </summary>
public static class EmailServiceExtensions
{
    public static async Task TrySendFromTemplateAsync(
        this IEmailService email,
        string to,
        string templateName,
        Dictionary<string, string> variables,
        ILogger logger,
        CancellationToken ct = default)
    {
        await email.SendFromTemplateAsync(to, templateName, variables, ct);
    }

    public static async Task TrySendAsync(
        this IEmailService email,
        string to,
        string subject,
        string htmlBody,
        ILogger logger,
        CancellationToken ct = default)
    {
        await email.SendAsync(to, subject, htmlBody, ct);
    }
}
