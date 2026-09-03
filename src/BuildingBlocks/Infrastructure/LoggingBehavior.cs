using System.Diagnostics;
using BuildingBlocks.Infrastructure.CurrentUser;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger, ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = currentUser.UserId ?? "anonymous";
        var traceId = Activity.Current?.TraceId.ToString();

        var stopwatch = Stopwatch.StartNew();

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestName"] = requestName,
            ["UserId"] = userId,
            // ["TraceId"] = traceId
        });
        
        logger.LogInformation(
            "Handling request {RequestName}",
            requestName);

        try
        {
            var response = await next(cancellationToken);

            stopwatch.Stop();

            logger.LogInformation(
                "Handled request {RequestName} successfully in {Duration}ms | Success: {Success}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                true);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            logger.LogError(
                ex,
                "Request {RequestName} failed after {Duration}ms | Success: {Success}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                false);

            throw;
        }
    }
}