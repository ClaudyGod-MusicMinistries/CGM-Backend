using ClaudyGod.Domain.Enums;
using ClaudyGod.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClaudyGod.Infrastructure.Services;

public sealed class DataRetentionWorker(
    IServiceScopeFactory scopes, IConfiguration configuration, ILogger<DataRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(Math.Max(1, configuration.GetValue("Retention:IntervalHours", 24)));
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await PurgeAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Scheduled data-retention pass failed."); }
            await Task.Delay(interval, stoppingToken);
        }
    }

    internal async Task PurgeAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var auditCutoff = now.AddDays(-configuration.GetValue("Retention:AuditLogDays", 365));
        var tokenCutoff = now.AddDays(-configuration.GetValue("Retention:ExpiredTokenGraceDays", 30));
        var uploadCutoff = now.AddDays(-configuration.GetValue("Retention:UploadSessionDays", 30));
        var outboxCutoff = now.AddDays(-configuration.GetValue("Retention:ProcessedOutboxDays", 30));

        var audit = await db.AuditLogs.Where(x => x.Timestamp < auditCutoff).ExecuteDeleteAsync(ct);
        var tokens = await db.RefreshTokens.IgnoreQueryFilters()
            .Where(x => x.ExpiresAt < tokenCutoff || x.IsRevoked && x.RevokedAt < tokenCutoff)
            .ExecuteDeleteAsync(ct);
        var uploads = await db.UploadSessions.IgnoreQueryFilters()
            .Where(x => x.ExpiresAt < uploadCutoff ||
                x.Status != UploadSessionStatus.Issued && x.CompletedAt < uploadCutoff)
            .ExecuteDeleteAsync(ct);
        var outbox = await db.OutboxMessages.Where(x => x.ProcessedAt < outboxCutoff).ExecuteDeleteAsync(ct);

        logger.LogInformation(
            "Retention completed: {AuditLogs} audit logs, {RefreshTokens} refresh tokens, {UploadSessions} upload sessions, {OutboxMessages} outbox messages removed.",
            audit, tokens, uploads, outbox);
    }
}
