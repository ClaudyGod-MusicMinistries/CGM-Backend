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
/// Fire-and-forget email helpers that log failures without propagating exceptions.
/// Use these in command handlers so email errors never fail the primary operation.
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
        try
        {
            await email.SendFromTemplateAsync(to, templateName, variables, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email notification failed for template '{Template}' to {Recipient}",
                templateName, to);
        }
    }

    public static async Task TrySendAsync(
        this IEmailService email,
        string to,
        string subject,
        string htmlBody,
        ILogger logger,
        CancellationToken ct = default)
    {
        try
        {
            await email.SendAsync(to, subject, htmlBody, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email notification failed with subject '{Subject}' to {Recipient}",
                subject, to);
        }
    }
}
