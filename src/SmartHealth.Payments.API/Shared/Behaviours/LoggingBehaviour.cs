using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SmartHealth.Payments.Shared.Behaviours;

/// <summary>
/// MediatR pipeline behaviour for structured logging and performance tracing.
/// </summary>
public sealed class LoggingBehaviour<TRequest, TResponse>(
    ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        logger.LogInformation("Handling {Request}", requestName);
        try
        {
            var response = await next();
            sw.Stop();
            logger.LogInformation("Handled {Request} in {ElapsedMs} ms", requestName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Error handling {Request} after {ElapsedMs} ms", requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
