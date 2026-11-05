using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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

        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        var problem = new ProblemDetailsResponse
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = exception switch
            {
                _ when statusCode == StatusCodes.Status500InternalServerError => "An unexpected error occurred.",
                DbUpdateConcurrencyException => "The record was changed by another request. Reload it and try again.",
                _ => exception.Message
            },
            TraceId = traceId,
            Errors = exception is ValidationException ve ? ve.Errors : null,
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Request failed. EventName={EventName}, ExceptionType={ExceptionType}, Message={Message}, StatusCode={StatusCode}, Response={@Response}",
                "request.failed",
                exception.GetType().Name,
                exception.Message,
                statusCode,
                problem);
        }
        else
        {
            _logger.LogWarning(
                "Request rejected. EventName={EventName}, ExceptionType={ExceptionType}, Message={Message}, StatusCode={StatusCode}, Response={@Response}",
                "request.rejected",
                exception.GetType().Name,
                exception.Message,
                statusCode,
                problem);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(problem, JsonOptions),
            cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, string Type)
        MapException(Exception exception) =>
        exception switch
        {
            ValidationException =>
                (400, "Validation Failed",
                    "https://tools.ietf.org/html/rfc7231#section-6.5.1"),

            NotFoundException =>
                (404, "Not Found",
                    "https://tools.ietf.org/html/rfc7231#section-6.5.4"),

            ConflictException =>
                (409, "Conflict",
                    "https://tools.ietf.org/html/rfc7231#section-6.5.8"),

            // Optimistic concurrency (xmin) — someone else changed the record first; the client should retry
            DbUpdateConcurrencyException =>
                (409, "Conflict",
                    "https://tools.ietf.org/html/rfc7231#section-6.5.8"),

            ForbiddenException =>
                (403, "Forbidden",
                    "https://tools.ietf.org/html/rfc7231#section-6.5.3"),

            BadHttpRequestException =>
                (400, "Bad Request",
                    "https://tools.ietf.org/html/rfc7231#section-6.5.1"),

            _ =>
                (500, "Server Error",
                    "https://tools.ietf.org/html/rfc7231#section-6.6.1")
        };
}