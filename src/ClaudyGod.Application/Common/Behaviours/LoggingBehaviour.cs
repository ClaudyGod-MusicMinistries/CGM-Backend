using MediatR;
using Microsoft.Extensions.Logging;

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

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        _logger.LogInformation("ClaudyGod Request: {Name} completed in {ElapsedMs}ms",
            requestName, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
