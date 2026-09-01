using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Exceptions;

/// <summary>
/// Implements <see cref="IExceptionHandler"/> (.NET 8+) to catch every unhandled exception,
/// map it to the correct HTTP status, and return a consistent RFC 7807 problem-details body.
///
/// Registration (Program.cs):
///   builder.Services.AddExceptionHandler&lt;GlobalExceptionHandler&gt;();
///   builder.Services.AddProblemDetails();
///   ...
///   app.UseExceptionHandler();
/// </summary>
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

        _logger.LogError(exception,
            "Unhandled exception — {ExceptionType}: {Message}",
            exception.GetType().Name, exception.Message);

        var problem = new ProblemDetailsResponse
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = exception.Message,
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
            ValidationException   => (StatusCodes.Status400BadRequest,  "Validation Failed",   "https://tools.ietf.org/html/rfc7231#section-6.5.1"),
            NotFoundException     => (StatusCodes.Status404NotFound,    "Not Found",            "https://tools.ietf.org/html/rfc7231#section-6.5.4"),
            ConflictException     => (StatusCodes.Status409Conflict,    "Conflict",             "https://tools.ietf.org/html/rfc7231#section-6.5.8"),
            ForbiddenException    => (StatusCodes.Status403Forbidden,   "Forbidden",            "https://tools.ietf.org/html/rfc7231#section-6.5.3"),
            _                     => (StatusCodes.Status500InternalServerError, "Server Error",  "https://tools.ietf.org/html/rfc7231#section-6.6.1"),
        };
}