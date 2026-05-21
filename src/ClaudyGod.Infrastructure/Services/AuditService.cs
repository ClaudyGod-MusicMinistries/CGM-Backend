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
                UserId = _currentUser.UserId ?? "anonymous",
                UserEmail = _currentUser.UserEmail ?? string.Empty,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
                NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues),
                IpAddress = _currentUser.IpAddress ?? string.Empty,
                UserAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty,
                Timestamp = DateTime.UtcNow,
                Succeeded = succeeded,
                FailureReason = failureReason
            };

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for action {Action}", action);
        }
    }
}
