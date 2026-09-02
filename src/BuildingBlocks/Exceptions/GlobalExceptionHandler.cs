using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, type) = MapException(exception);

        var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString();

        _logger.LogError(
            exception,
            "Unhandled exception | TraceId: {{TraceId}} | {ExceptionType}: {Message}",
            traceId,
            exception.GetType().Name);

        var problem = new ProblemDetailsResponse
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : exception.Message,
            TraceId = traceId,
            Errors = exception is ValidationException ve ? ve.Errors : null,
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(problem, JsonOptions),
            cancellationToken);

        return true; // we handled it — short-circuit the pipeline
    }

    private static (int StatusCode, string Title, string Type) MapException(Exception exception) =>
        exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation Failed",
                "https://tools.ietf.org/html/rfc7231#section-6.5.1"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found",
                "https://tools.ietf.org/html/rfc7231#section-6.5.4"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict",
                "https://tools.ietf.org/html/rfc7231#section-6.5.8"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden",
                "https://tools.ietf.org/html/rfc7231#section-6.5.3"),
            _ => (StatusCodes.Status500InternalServerError, "Server Error",
                "https://tools.ietf.org/html/rfc7231#section-6.6.1"),
        };
}