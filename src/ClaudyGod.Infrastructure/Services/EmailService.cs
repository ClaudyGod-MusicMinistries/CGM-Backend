using ClaudyGod.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace ClaudyGod.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    private string SmtpHost => _config["Email:SmtpHost"] ?? "smtp.gmail.com";
    private int SmtpPort => int.TryParse(_config["Email:SmtpPort"], out var p) ? p : 587;
    private string SmtpUsername => _config["Email:SmtpUsername"] ?? string.Empty;
    private string SmtpPassword => _config["Email:SmtpPassword"] ?? string.Empty;
    private string SenderEmail => _config["Email:SenderEmail"] ?? SmtpUsername;
    private string SenderName => _config["Email:SenderName"] ?? "ClaudyGod Ministry";
    private bool UsePostfix => bool.TryParse(_config["Email:UsePostfix"], out var b) && b;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var message = BuildMessage(to, subject, htmlBody);
        await SendMessageAsync(message, ct);
    }

    public async Task SendFromTemplateAsync(string to, string templateName,
        Dictionary<string, string> variables, CancellationToken ct = default)
    {
        var html = await LoadTemplateAsync(templateName, variables);
        var subject = variables.TryGetValue("subject", out var s) ? s : "Message from ClaudyGod";
        await SendAsync(to, subject, html, ct);
    }

    public async Task SendWithAttachmentAsync(string to, string subject, string htmlBody,
        Stream attachment, string attachmentName, string attachmentMimeType, CancellationToken ct = default)
    {
        var message = BuildMessage(to, subject, htmlBody);

        var part = new MimePart(attachmentMimeType)
        {
            Content = new MimeContent(attachment),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64,
            FileName = attachmentName
        };

        var multipart = new Multipart("mixed");
        multipart.Add(message.Body);
        multipart.Add(part);
        message.Body = multipart;

        await SendMessageAsync(message, ct);
    }

    private MimeMessage BuildMessage(string to, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(SenderName, SenderEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = HtmlToPlainText(htmlBody)
        };

        message.Body = builder.ToMessageBody();
        return message;
    }

    private async Task SendMessageAsync(MimeMessage message, CancellationToken ct)
    {
        using var client = new SmtpClient();

        try
        {
            if (UsePostfix)
            {
                var postfixHost = _config["Email:PostfixHost"] ?? "localhost";
                var postfixPort = int.TryParse(_config["Email:PostfixPort"], out var pp) ? pp : 25;
                await client.ConnectAsync(postfixHost, postfixPort, SecureSocketOptions.None, ct);
            }
            else
            {
                var socketOption = SmtpPort == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

                await client.ConnectAsync(SmtpHost, SmtpPort, socketOption, ct);
                await client.AuthenticateAsync(SmtpUsername, SmtpPassword, ct);
            }

            await client.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient} with subject: {Subject}",
                message.To, message.Subject);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
    }

    private async Task<string> LoadTemplateAsync(string templateName,
        Dictionary<string, string> variables)
    {
        var assembly = typeof(EmailService).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith($"{templateName}.html", StringComparison.OrdinalIgnoreCase));

        string html;

        if (resourceName is not null)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            html = await reader.ReadToEndAsync();
        }
        else
        {
            _logger.LogWarning("Email template '{Template}' not found; using fallback body.", templateName);
            html = variables.TryGetValue("body", out var b) ? b : string.Empty;
        }

        foreach (var (key, value) in variables)
            html = html.Replace($"{{{{{key}}}}}", value, StringComparison.OrdinalIgnoreCase);

        return html;
    }

    private static string HtmlToPlainText(string html)
    {
        // Minimal stripping — good enough for fallback plain-text part
        return System.Text.RegularExpressions.Regex
            .Replace(html, "<[^>]+>", string.Empty)
            .Trim();
    }
}
