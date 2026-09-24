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
        var requestName = typeof(TRequest).Name;
        var userId = currentUser.UserId ?? "anonymous";

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["RequestName"] = requestName,
            ["UserId"] = userId
        });

        logger.LogInformation(
            "Handling request {RequestName}",
            requestName);

        try
        {
            var response = await next(cancellationToken);

            logger.LogInformation(
                "Handled request {RequestName} successfully",
                requestName);

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Request {RequestName} failed",
                requestName);

            throw;
        }
    }
}