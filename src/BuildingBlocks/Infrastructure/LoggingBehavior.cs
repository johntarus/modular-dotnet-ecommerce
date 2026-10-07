using System.Diagnostics;
using BuildingBlocks.Infrastructure.CurrentUser;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName =
            typeof(TRequest).Name;

        var userId =
            currentUser.UserId ?? "anonymous";

        using var scope =
            logger.BeginScope(
                new Dictionary<string, object?>
                {
                    ["RequestName"] = requestName,
                    ["UserId"] = userId
                });

        var stopwatch =
            Stopwatch.StartNew();

        logger.LogDebug(
            "Request started. EventName={EventName}",
            "request.started");

        try
        {
            var response =
                await next(cancellationToken);

            stopwatch.Stop();

            logger.LogInformation(
                "Request completed. EventName={EventName}, DurationMs={DurationMs}",
                "request.completed",
                stopwatch.Elapsed.TotalMilliseconds);

            return response;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            logger.LogInformation(
                "Request cancelled. EventName={EventName}, DurationMs={DurationMs}",
                "request.cancelled",
                stopwatch.Elapsed.TotalMilliseconds);

            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            logger.LogError(
                ex,
                "Request failed. EventName={EventName}, DurationMs={DurationMs}",
                "request.failed",
                stopwatch.Elapsed.TotalMilliseconds);

            throw;
        }
    }
}