using System.Text.Json;
using ClaudyGod.Domain.Events;
using ClaudyGod.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClaudyGod.Infrastructure.Services;

public sealed class OutboxProcessor(IServiceScopeFactory scopes, ILogger<OutboxProcessor> logger) : BackgroundService
{
    private readonly string _workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        do
        {
            try { await ProcessBatchAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Outbox batch failed; delivery will retry."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var messages = await db.OutboxMessages
            .Where(x => x.ProcessedAt == null && x.AvailableAt <= now &&
                        (x.LockedUntil == null || x.LockedUntil < now))
            .OrderBy(x => x.OccurredAt).Take(25).ToListAsync(ct);

        foreach (var message in messages)
        {
            message.LockOwner = _workerId;
            message.LockedUntil = now.AddMinutes(2);
        }
        if (messages.Count == 0) return;
        await db.SaveChangesAsync(ct);

        var sender = scope.ServiceProvider.GetRequiredService<EmailService>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        foreach (var message in messages)
        {
            try
            {
                if (message.Kind == "email") await DeliverEmailAsync(sender, message.Type, message.Payload, ct);
                else if (message.Kind == "domain-event") await PublishEventAsync(mediator, message.Type, message.Payload, ct);
                else throw new InvalidOperationException($"Unknown outbox kind '{message.Kind}'.");
                message.ProcessedAt = DateTime.UtcNow;
                message.LastError = null;
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.LastError = ex.ToString()[..Math.Min(ex.ToString().Length, 4000)];
                message.AvailableAt = DateTime.UtcNow.AddSeconds(Math.Min(3600, Math.Pow(2, message.Attempts) * 10));
                logger.LogError(ex, "Outbox message {OutboxId} delivery attempt {Attempt} failed.", message.Id, message.Attempts);
            }
            finally { message.LockOwner = null; message.LockedUntil = null; }
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task DeliverEmailAsync(EmailService sender, string type, string payload, CancellationToken ct)
    {
        if (type == "template")
        {
            var email = JsonSerializer.Deserialize<OutboxEmailService.TemplateEmail>(payload)!;
            await sender.SendFromTemplateAsync(email.To, email.TemplateName, email.Variables, ct);
            return;
        }
        var html = JsonSerializer.Deserialize<OutboxEmailService.HtmlEmail>(payload)!;
        await sender.SendAsync(html.To, html.Subject, html.HtmlBody, ct);
    }

    private static async Task PublishEventAsync(IMediator mediator, string typeName, string payload, CancellationToken ct)
    {
        var type = Type.GetType(typeName, throwOnError: true)!;
        if (!typeof(IDomainEvent).IsAssignableFrom(type) || type.Assembly != typeof(IDomainEvent).Assembly)
            throw new InvalidOperationException("Outbox event type is not an approved domain event.");
        var domainEvent = JsonSerializer.Deserialize(payload, type)
            ?? throw new JsonException("Domain event payload was empty.");
        await mediator.Publish(domainEvent, ct);
    }
}
