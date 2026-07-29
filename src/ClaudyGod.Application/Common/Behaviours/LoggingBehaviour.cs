using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ClaudyGod.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("ClaudyGod Request: {Name}", requestName);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next();
            _logger.LogInformation(
                "Application request {RequestName} completed in {ElapsedMs}ms TraceId={TraceId}",
                requestName, stopwatch.ElapsedMilliseconds, Activity.Current?.TraceId.ToString());
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Application request {RequestName} failed after {ElapsedMs}ms TraceId={TraceId} ExceptionType={ExceptionType}",
                requestName, stopwatch.ElapsedMilliseconds, Activity.Current?.TraceId.ToString(), ex.GetType().Name);
            throw;
        }
    }
}
