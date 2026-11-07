using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting; 
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; 

namespace BuildingBlocks.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment env)           
    {
        _logger = logger;
        _env = env;
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
            Detail = GetDetail(exception, statusCode), 
            TraceId = traceId,
            Errors = exception is ValidationException ve ? ve.Errors : null,
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception,
                "Request failed. EventName={EventName}, ExceptionType={ExceptionType}, Message={Message}, StatusCode={StatusCode}, Response={@Response}",
                "request.failed", exception.GetType().Name, exception.Message, statusCode, problem);
        }
        else
        {
            _logger.LogWarning(
                "Request rejected. EventName={EventName}, ExceptionType={ExceptionType}, Message={Message}, StatusCode={StatusCode}, Response={@Response}",
                "request.rejected", exception.GetType().Name, exception.Message, statusCode, problem);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions), cancellationToken);
        return true;
    }

    // ──────────────────────────────────────────────────────────
    // NEW: Environment-aware detail. Safe curated messages for
    // known cases; raw message in dev for 5xx; generic in prod.
    // ──────────────────────────────────────────────────────────
    private string GetDetail(Exception exception, int statusCode)
    {
        // Curated, safe messages — visible in all environments
        switch (exception)
        {
            case DbUpdateConcurrencyException:
                return "The record was changed by another request. Reload it and try again.";

            case DbUpdateException when IsUniqueViolation(exception):
                return "A record with the same unique value already exists.";

            case UnauthorizedAccessException:
                return "You are not authorized to perform this action.";

            case OptionsValidationException:
                // Config error — dev should see what's missing, prod gets a safe message
                return _env.IsDevelopment()
                    ? exception.Message
                    : "The server is misconfigured. Please contact support.";

            case ValidationException ve:
                return ve.Message;
        }

        // 5xx — server-side errors
        if (statusCode >= 500)
        {
            // Dev: expose the real error so you can debug it
            // Prod: hide internals (information disclosure risk)
            return _env.IsDevelopment()
                ? $"{exception.GetType().Name}: {exception.Message}"
                : "An unexpected error occurred.";
        }

        // 4xx — client errors, message is safe to expose
        return exception.Message;
    }

    private static (int StatusCode, string Title, string Type) MapException(Exception exception) =>
        exception switch
        {
            // ── 400s ──
            ValidationException =>
                (400, "Validation Failed", "https://tools.ietf.org/html/rfc7231#section-6.5.1"),

            // Catches System.ComponentModel.DataAnnotations.ValidationException
            // (thrown from ShoppingCart.AddItem / SetItemQuantity / etc.)
            System.ComponentModel.DataAnnotations.ValidationException =>
                (400, "Validation Failed", "https://tools.ietf.org/html/rfc7231#section-6.5.1"),

            BadHttpRequestException =>
                (400, "Bad Request", "https://tools.ietf.org/html/rfc7231#section-6.5.1"),

            ArgumentException =>
                (400, "Bad Request", "https://tools.ietf.org/html/rfc7231#section-6.5.1"),

            // ── 401 / 403 ──
            UnauthorizedAccessException =>
                (401, "Unauthorized", "https://tools.ietf.org/html/rfc7235#section-3.1"),

            ForbiddenException =>
                (403, "Forbidden", "https://tools.ietf.org/html/rfc7231#section-6.5.3"),

            // ── 404 ──
            NotFoundException =>
                (404, "Not Found", "https://tools.ietf.org/html/rfc7231#section-6.5.4"),

            // ── 409 (conflicts) ──
            ConflictException =>
                (409, "Conflict", "https://tools.ietf.org/html/rfc7231#section-6.5.8"),

            DbUpdateConcurrencyException =>
                (409, "Conflict", "https://tools.ietf.org/html/rfc7231#section-6.5.8"),

            DbUpdateException when IsUniqueViolation(exception) =>
                (409, "Conflict", "https://tools.ietf.org/html/rfc7231#section-6.5.8"),

            // ── 500 ──
            OptionsValidationException =>
                (500, "Server Error", "https://tools.ietf.org/html/rfc7231#section-6.6.1"),

            _ =>
                (500, "Server Error", "https://tools.ietf.org/html/rfc7231#section-6.6.1")
        };

    private static bool IsUniqueViolation(Exception exception) =>
        exception.InnerException is System.Data.Common.DbException { SqlState: "23505" };
}