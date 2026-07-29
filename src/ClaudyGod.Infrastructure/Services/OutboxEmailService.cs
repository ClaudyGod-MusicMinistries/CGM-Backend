using System.Text.Json;
using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Entities;
using ClaudyGod.Infrastructure.Persistence;

namespace ClaudyGod.Infrastructure.Services;

public sealed class OutboxEmailService(ApplicationDbContext db) : IEmailService
{
    public Task SendFromTemplateAsync(string to, string templateName,
        Dictionary<string, string> variables, CancellationToken ct = default)
    {
        Enqueue("template", new TemplateEmail(to, templateName, variables));
        return Task.CompletedTask;
    }

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        Enqueue("html", new HtmlEmail(to, subject, htmlBody));
        return Task.CompletedTask;
    }

    public Task SendWithAttachmentAsync(string to, string subject, string htmlBody, Stream attachment,
        string attachmentName, string attachmentMimeType, CancellationToken ct = default) =>
        throw new NotSupportedException("Attachments must be stored durably before they can be queued.");

    private void Enqueue<T>(string type, T payload)
    {
        var now = DateTime.UtcNow;
        db.OutboxMessages.Add(new OutboxMessage
        {
            Kind = "email",
            Type = type,
            Payload = JsonSerializer.Serialize(payload),
            OccurredAt = now,
            AvailableAt = now
        });
    }

    internal sealed record TemplateEmail(string To, string TemplateName, Dictionary<string, string> Variables);
    internal sealed record HtmlEmail(string To, string Subject, string HtmlBody);
}
