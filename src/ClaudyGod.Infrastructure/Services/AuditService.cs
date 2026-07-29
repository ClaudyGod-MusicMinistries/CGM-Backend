using System.Text.Json;
using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ClaudyGod.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IApplicationDbContext db, ICurrentUserService currentUser,
        IHttpContextAccessor httpContextAccessor, ILogger<AuditService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(string action, string entityType, string? entityId = null,
        object? oldValues = null, object? newValues = null,
        bool succeeded = true, string? failureReason = null,
        CancellationToken ct = default)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = Truncate(_currentUser.UserId ?? "anonymous", 100),
                UserEmail = Truncate(_currentUser.UserEmail ?? string.Empty, 256),
                Action = Truncate(action, 100),
                EntityType = Truncate(entityType, 100),
                EntityId = entityId is null ? null : Truncate(entityId, 100),
                OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
                NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues),
                IpAddress = Truncate(_currentUser.IpAddress ?? string.Empty, 50),
                UserAgent = Truncate(
                    _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty, 512),
                Timestamp = DateTime.UtcNow,
                Succeeded = succeeded,
                FailureReason = failureReason is null ? null : Truncate(failureReason, 500)
            };

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for action {Action}", action);
        }
    }

    private static string Truncate(string value, int maximum) =>
        value.Length <= maximum ? value : value[..maximum];
}
