using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var traceId = Activity.Current?.TraceId.ToString();

        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation(
            "Handling request {RequestName} | TraceId: {TraceId}",
            requestName,
            traceId);

        try
        {
            var response = await next(cancellationToken);

            stopwatch.Stop();

            logger.LogInformation(
                "Handled request {RequestName} successfully in {Duration}ms | TraceId: {TraceId}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                traceId);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            logger.LogError(
                ex,
                "Request {RequestName} failed after {Duration}ms | TraceId: {TraceId}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                traceId);

            throw;
        }
    }
}