using System.Text.Json;
using BuildingBlocks.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using ValidationException = BuildingBlocks.Exceptions.ValidationException;

namespace Api.IntegrationTests.BuildingBlocks;

/// <summary>
/// Exception → HTTP status mapping. Concurrency conflicts can't be raced reliably over HTTP,
/// so the mapping is checked directly here.
/// </summary>
public class GlobalExceptionHandlerTests
{
    private static async Task<(int Status, JsonElement Body)> HandleAsync(
        Exception exception,
        string environmentName = "Production")   // ← default to Production (safe behavior)
    {
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var env = new FakeHostEnvironment(environmentName);
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, env);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        context.Response.Body.Position = 0;
        return (context.Response.StatusCode, JsonDocument.Parse(context.Response.Body).RootElement);
    }

    // ──────────────────────────────────────────────────────────
    // Status code mappings
    // ──────────────────────────────────────────────────────────
    public static TheoryData<Exception, int> Mappings => new()
    {
        // 400
        { new ValidationException("bad"), 400 },
        { new System.ComponentModel.DataAnnotations.ValidationException("quantity must be > 0"), 400 },
        { new BadHttpRequestException("bad"), 400 },
        { new ArgumentException("price must be > 0"), 400 },
        { new ArgumentNullException("name"), 400 },

        // 401
        { new UnauthorizedAccessException("no token"), 401 },

        // 403
        { new ForbiddenException("no"), 403 },

        // 404
        { new NotFoundException("missing"), 404 },

        // 409
        { new ConflictException("clash"), 409 },
        { new DbUpdateConcurrencyException("expected to affect 1 row(s), but actually affected 0 row(s)"), 409 },
        { new DbUpdateException("save failed", new PostgresException("duplicate key", "ERROR", "ERROR", "23505")), 409 },

        // 500 — DbUpdateException that isn't a unique violation
        { new DbUpdateException("save failed", new PostgresException("not null", "ERROR", "ERROR", "23502")), 500 },

        // 500 — unknown exception types
        { new InvalidOperationException("boom"), 500 },
        { new TimeoutException("upstream timed out"), 500 },
    };

    [Theory]
    [MemberData(nameof(Mappings))]
    public async Task MapsExceptionToStatus(Exception exception, int expected)
    {
        var (status, body) = await HandleAsync(exception);

        Assert.Equal(expected, status);
        Assert.Equal(expected, body.GetProperty("status").GetInt32());
    }

    // ──────────────────────────────────────────────────────────
    // Production: sensitive details hidden
    // ──────────────────────────────────────────────────────────
    [Fact]
    public async Task ConcurrencyConflict_HidesEfInternalsFromClient()
    {
        var (_, body) = await HandleAsync(
            new DbUpdateConcurrencyException("expected to affect 1 row(s), but actually affected 0 row(s)"),
            environmentName: "Production");

        var detail = body.GetProperty("detail").GetString();
        Assert.DoesNotContain("row(s)", detail);
        Assert.Contains("try again", detail);
    }

    [Fact]
    public async Task UniqueViolation_HidesDatabaseDetailsFromClient()
    {
        var (_, body) = await HandleAsync(
            new DbUpdateException("save failed",
                new PostgresException(
                    "duplicate key value violates unique constraint \"IX_categories_active_name\"",
                    "ERROR", "ERROR", "23505")),
            environmentName: "Production");

        Assert.Equal("A record with the same unique value already exists.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ServerError_HidesExceptionMessage_InProduction()
    {
        var (_, body) = await HandleAsync(
            new InvalidOperationException("connection string Password=secret"),
            environmentName: "Production");

        Assert.Equal("An unexpected error occurred.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task OptionsValidationError_HidesConfigDetails_InProduction()
    {
        var (status, body) = await HandleAsync(
            new Microsoft.Extensions.Options.OptionsValidationException(
                "AiOptions", 
                typeof(object), 
                new[] { "Ai:ApiKey is required. Set Ai__ApiKey env var or add to appsettings." }),
            environmentName: "Production");

        Assert.Equal(500, status);
        Assert.Equal("The server is misconfigured. Please contact support.", body.GetProperty("detail").GetString());
    }

    // ──────────────────────────────────────────────────────────
    // Development: real errors visible for debugging
    // ──────────────────────────────────────────────────────────
    [Fact]
    public async Task ServerError_ShowsRealExceptionMessage_InDevelopment()
    {
        var (_, body) = await HandleAsync(
            new InvalidOperationException("Ai:ApiKey is not configured."),
            environmentName: "Development");

        var detail = body.GetProperty("detail").GetString();
        Assert.Contains("InvalidOperationException", detail);
        Assert.Contains("Ai:ApiKey is not configured", detail);
    }

    [Fact]
    public async Task OptionsValidationError_ShowsRawMessage_InDevelopment()
    {
        var (_, body) = await HandleAsync(
            new Microsoft.Extensions.Options.OptionsValidationException(
                "AiOptions", 
                typeof(object), 
                new[] { "Ai:ApiKey is required. Set Ai__ApiKey env var or add to appsettings." }),
            environmentName: "Development");

        Assert.Contains("Ai:ApiKey is required", body.GetProperty("detail").GetString());
    }

    // ──────────────────────────────────────────────────────────
    // Content negotiation
    // ──────────────────────────────────────────────────────────
    [Fact]
    public async Task Response_UsesProblemJsonContentType()
    {
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance,
            new FakeHostEnvironment("Production"));

        await handler.TryHandleAsync(context, new NotFoundException("x"), CancellationToken.None);

        Assert.Equal("application/problem+json", context.Response.ContentType);
    }

    [Fact]
    public async Task Response_AlwaysIncludesTraceId()
    {
        var (_, body) = await HandleAsync(new NotFoundException("x"));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));
    }

    // ──────────────────────────────────────────────────────────
    // Minimal IHostEnvironment stub — no mocking library needed
    // ──────────────────────────────────────────────────────────
    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}