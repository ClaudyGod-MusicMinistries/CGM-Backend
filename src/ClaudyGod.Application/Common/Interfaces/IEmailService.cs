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
