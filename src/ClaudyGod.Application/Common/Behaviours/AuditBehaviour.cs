using ClaudyGod.Application.Common.Interfaces;
using MediatR;

namespace ClaudyGod.Application.Common.Behaviours;

/// <summary>
/// Records command outcomes without serializing request bodies, passwords, or
/// personal data. Queries are excluded because request logging already covers
/// operational reads and the audit table is intended for state changes.
/// </summary>
public sealed class AuditBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAuditService _audit;

    public AuditBehaviour(IAuditService audit) => _audit = audit;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        if (!requestName.EndsWith("Command", StringComparison.Ordinal))
            return await next();

        var feature = typeof(TRequest).Namespace?.Split('.').Reverse().Skip(1).FirstOrDefault()
                      ?? "Application";
        try
        {
            var response = await next();
            await _audit.LogAsync(requestName, feature, succeeded: true, ct: cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            await _audit.LogAsync(requestName, feature, succeeded: false,
                failureReason: ex.GetType().Name, ct: cancellationToken);
            throw;
        }
    }
}
