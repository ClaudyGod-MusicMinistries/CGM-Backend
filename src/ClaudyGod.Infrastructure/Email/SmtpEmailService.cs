using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClaudyGod.Infrastructure.Email;

/// <summary>
/// SMTP implementation of email service
/// Uses System.Net.Mail for sending emails
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly SmtpClient _smtpClient;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;

        var smtpHost = config["Email:SmtpHost"] ?? "localhost";
        var smtpPort = int.Parse(config["Email:SmtpPort"] ?? "587");
        var smtpUsername = config["Email:SmtpUsername"] ?? "";
        var smtpPassword = config["Email:SmtpPassword"] ?? "";
        var enableSsl = bool.Parse(config["Email:EnableSsl"] ?? "true");

        _fromEmail = config["Email:SenderEmail"] ?? "noreply@claudygod.org";
        _fromName = config["Email:SenderName"] ?? "ClaudyGod Music Ministries";

        _smtpClient = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = !string.IsNullOrEmpty(smtpUsername) ? new NetworkCredential(smtpUsername, smtpPassword) : null,
            Timeout = 10000,
        };

        _logger.LogInformation(
            "[Email] SMTP configured: {Host}:{Port} (SSL={Ssl})",
            smtpHost,
            smtpPort,
            enableSsl
        );
    }

    /// <summary>
    /// Send email with proper HTML formatting and branding
    /// </summary>
    public async Task<bool> SendEmailAsync(EmailMessage message, CancellationToken ct = default)
    {
        try
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = message.Subject,
                IsBodyHtml = true,
                Body = message.HtmlContent,
            };

            // Add To address
            mailMessage.To.Add(new MailAddress(message.To));

            // Add ReplyTo if specified
            if (!string.IsNullOrEmpty(message.ReplyTo))
            {
                mailMessage.ReplyToList.Add(new MailAddress(message.ReplyTo));
            }

            // Add alternative text version if provided
            if (!string.IsNullOrEmpty(message.TextContent))
            {
                var plainTextView = AlternateView.CreateAlternateViewFromString(
                    message.TextContent,
                    null,
                    "text/plain"
                );
                mailMessage.AlternateViews.Add(plainTextView);
            }

            // Add custom headers
            if (message.Tags != null)
            {
                foreach (var tag in message.Tags)
                {
                    mailMessage.Headers.Add($"X-{tag.Key}", tag.Value);
                }
            }

            await _smtpClient.SendMailAsync(mailMessage, ct);

            _logger.LogInformation("[Email] Sent to {To}: {Subject}", message.To, message.Subject);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[Email] Send operation cancelled for {To}", message.To);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] Failed to send to {To}: {Subject}", message.To, message.Subject);
            return false;
        }
    }

    public Task<bool> SendNewsletterWelcomeAsync(string email, CancellationToken ct = default)
    {
        var message = new EmailMessage
        {
            To = email,
            Subject = "Welcome to ClaudyGod Music Ministries! 🎵",
            HtmlContent = GetNewsletterWelcomeHtml(email),
            TextContent = GetNewsletterWelcomeText(email),
            Tags = new() { { "Type", "Newsletter" }, { "Event", "WelcomeEmail" } },
        };

        return SendEmailAsync(message, ct);
    }

    public Task<bool> SendEventRegistrationAsync(
        string email,
        string name,
        string eventTitle,
        DateTime eventDate,
        CancellationToken ct = default
    )
    {
        var formattedDate = eventDate.ToString("dddd, MMMM d, yyyy");
        var message = new EmailMessage
        {
            To = email,
            Subject = $"Event Registration Confirmed: {eventTitle}",
            HtmlContent = GetEventRegistrationHtml(name, eventTitle, formattedDate),
            TextContent = GetEventRegistrationText(name, eventTitle, formattedDate),
            Tags = new() { { "Type", "Event" }, { "Event", eventTitle } },
        };

        return SendEmailAsync(message, ct);
    }

    public Task<bool> SendContactFormConfirmationAsync(
        string email,
        string name,
        string subject,
        CancellationToken ct = default
    )
    {
        var message = new EmailMessage
        {
            To = email,
            Subject = $"We Received Your Message - {subject}",
            HtmlContent = GetContactConfirmationHtml(name, subject),
            TextContent = GetContactConfirmationText(name, subject),
            Tags = new() { { "Type", "Contact" }, { "Subject", subject } },
        };

        return SendEmailAsync(message, ct);
    }

    public Task<bool> SendVolunteerApplicationAsync(
        string email,
        string name,
        string role,
        CancellationToken ct = default
    )
    {
        var message = new EmailMessage
        {
            To = email,
            Subject = $"Volunteer Application Received - {role}",
            HtmlContent = GetVolunteerApplicationHtml(name, role),
            TextContent = GetVolunteerApplicationText(name, role),
            Tags = new() { { "Type", "Volunteer" }, { "Role", role } },
        };

        return SendEmailAsync(message, ct);
    }

    private static string GetNewsletterWelcomeHtml(string email) =>
        $"""
        <!DOCTYPE html>
        <html>
          <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
          </head>
          <body style="margin: 0; padding: 0; background: #f5f5f5; font-family: 'Raleway', sans-serif;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background: linear-gradient(135deg, #0D0B1A 0%, #1a1628 100%);">
              <tr>
                <td align="center" style="padding: 40px 20px;">
                  <h1 style="margin: 0; font-family: 'Bricolage Grotesque', sans-serif; font-size: 24px; font-weight: bold; color: white;">ClaudyGod Music Ministries</h1>
                </td>
              </tr>
            </table>
            <table width="100%" cellpadding="0" cellspacing="0" style="background: white;">
              <tr>
                <td align="center" style="padding: 40px 20px;">
                  <div style="max-width: 600px;">
                    <h2 style="font-family: 'Bricolage Grotesque', sans-serif; font-size: 28px; color: #0D0B1A;">Welcome to Our Community! 🙏</h2>
                    <p style="font-size: 16px; color: #333; line-height: 1.8;">Thank you for subscribing, {email}!</p>
                    <p style="font-size: 14px; color: #666;">You'll receive the latest music, worship videos, and ministry updates.</p>
                  </div>
                </td>
              </tr>
            </table>
          </body>
        </html>
        """;

    private static string GetNewsletterWelcomeText(string email) =>
        $"Welcome to ClaudyGod Music Ministries!\n\nThank you for subscribing, {email}!\n\nYou'll receive the latest music, worship videos, and ministry updates.";

    private static string GetEventRegistrationHtml(string name, string eventTitle, string eventDate) =>
        $"""
        <!DOCTYPE html>
        <html>
          <body style="margin: 0; padding: 0; background: #f5f5f5; font-family: 'Raleway', sans-serif;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background: #0D0B1A;">
              <tr>
                <td align="center" style="padding: 40px 20px;">
                  <h1 style="margin: 0; color: white; font-family: 'Bricolage Grotesque', sans-serif;">ClaudyGod</h1>
                </td>
              </tr>
            </table>
            <table width="100%" cellpadding="0" cellspacing="0" style="background: white;">
              <tr>
                <td align="center" style="padding: 40px 20px;">
                  <div style="max-width: 600px;">
                    <h2 style="font-family: 'Bricolage Grotesque', sans-serif; font-size: 28px; color: #0D0B1A;">Registration Confirmed! ✓</h2>
                    <p style="font-size: 16px; color: #333;">Hi {name},</p>
                    <p style="color: #555;">Your registration for <strong>{eventTitle}</strong> has been confirmed!</p>
                    <div style="background: #f8f9fa; padding: 20px; margin: 20px 0; border-radius: 8px;">
                      <p style="margin: 0; color: #555;"><strong>Date:</strong> {eventDate}</p>
                      <p style="margin: 10px 0 0 0; color: #555;"><strong>Event:</strong> {eventTitle}</p>
                    </div>
                    <p style="font-size: 14px; color: #666;">We look forward to seeing you there! 🙏</p>
                  </div>
                </td>
              </tr>
            </table>
          </body>
        </html>
        """;

    private static string GetEventRegistrationText(string name, string eventTitle, string eventDate) =>
        $"Event Registration Confirmed!\n\nHi {name},\n\nYour registration for {eventTitle} has been confirmed!\n\nDate: {eventDate}\n\nWe look forward to seeing you there!";

    private static string GetContactConfirmationHtml(string name, string subject) =>
        $"""
        <!DOCTYPE html>
        <html>
          <body style="margin: 0; padding: 0; background: #f5f5f5; font-family: 'Raleway', sans-serif;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background: #0D0B1A;">
              <tr>
                <td align="center" style="padding: 40px 20px;">
                  <h1 style="margin: 0; color: white; font-family: 'Bricolage Grotesque', sans-serif;">ClaudyGod</h1>
                </td>
              </tr>
            </table>
            <table width="100%" cellpadding="0" cellspacing="0" style="background: white;">
              <tr>
                <td align="center" style="padding: 40px 20px;">
                  <div style="max-width: 600px;">
                    <h2 style="font-family: 'Bricolage Grotesque', sans-serif; font-size: 28px; color: #0D0B1A;">Thank You for Reaching Out! 📬</h2>
                    <p style="font-size: 16px; color: #333;">Hi {name},</p>
                    <p style="color: #555;">We've received your message about: <strong>{subject}</strong></p>
                    <p style="font-size: 14px; color: #666;">Our team will respond within 24-48 hours.</p>
                  </div>
                </td>
              </tr>
            </table>
          </body>
        </html>
        """;

    private static string GetContactConfirmationText(string name, string subject) =>
        $"Thank You for Reaching Out!\n\nHi {name},\n\nWe've received your message about: {subject}\n\nOur team will respond within 24-48 hours.";

    private static string GetVolunteerApplicationHtml(string name, string role) =>
        $"""
        <!DOCTYPE html>
        <html>
          <body style="margin: 0; padding: 0; background: #f5f5f5; font-family: 'Raleway', sans-serif;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background: #0D0B1A;">
              <tr>
                <td align="center" style="padding: 40px 20px;">
                  <h1 style="margin: 0; color: white; font-family: 'Bricolage Grotesque', sans-serif;">ClaudyGod</h1>
                </td>
              </tr>
            </table>
            <table width="100%" cellpadding="0" cellspacing="0" style="background: white;">
              <tr>
                <td align="center" style="padding: 40px 20px;">
                  <div style="max-width: 600px;">
                    <h2 style="font-family: 'Bricolage Grotesque', sans-serif; font-size: 28px; color: #0D0B1A;">Application Received! 🙌</h2>
                    <p style="font-size: 16px; color: #333;">Hi {name},</p>
                    <p style="color: #555;">Thank you for applying to volunteer as a <strong>{role}</strong>!</p>
                    <p style="font-size: 14px; color: #666;">Our team will contact you within 3-5 business days.</p>
                  </div>
                </td>
              </tr>
            </table>
          </body>
        </html>
        """;

    private static string GetVolunteerApplicationText(string name, string role) =>
        $"Volunteer Application Received!\n\nHi {name},\n\nThank you for applying to volunteer as a {role}!\n\nOur team will contact you within 3-5 business days.";
}
