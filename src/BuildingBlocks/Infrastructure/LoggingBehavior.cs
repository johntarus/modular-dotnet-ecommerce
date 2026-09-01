using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", name);
        var response = await next(cancellationToken);
        logger.LogInformation("Handled {RequestName}", name);
        return response;
    }
}
