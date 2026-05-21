namespace ClaudyGod.Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, string entityType, string? entityId = null,
        object? oldValues = null, object? newValues = null,
        bool succeeded = true, string? failureReason = null,
        CancellationToken ct = default);
}
