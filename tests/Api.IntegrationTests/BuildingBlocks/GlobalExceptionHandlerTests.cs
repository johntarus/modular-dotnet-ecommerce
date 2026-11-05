using System.Text.Json;
using BuildingBlocks.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ValidationException = BuildingBlocks.Exceptions.ValidationException;

namespace Api.IntegrationTests.BuildingBlocks;

/// <summary>
/// Exception → HTTP status mapping. Concurrency conflicts can't be raced reliably over HTTP,
/// so the mapping is checked directly here.
/// </summary>
public class GlobalExceptionHandlerTests
{
    private static async Task<(int Status, JsonElement Body)> HandleAsync(Exception exception)
    {
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var handled = await new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance)
            .TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        context.Response.Body.Position = 0;
        return (context.Response.StatusCode, JsonDocument.Parse(context.Response.Body).RootElement);
    }

    public static TheoryData<Exception, int> Mappings => new()
    {
        { new ValidationException("bad"), 400 },
        { new BadHttpRequestException("bad"), 400 },
        { new ForbiddenException("no"), 403 },
        { new NotFoundException("missing"), 404 },
        { new ConflictException("clash"), 409 },
        { new DbUpdateConcurrencyException("expected to affect 1 row(s), but actually affected 0 row(s)"), 409 },
        { new InvalidOperationException("boom"), 500 },
    };

    [Theory]
    [MemberData(nameof(Mappings))]
    public async Task MapsExceptionToStatus(Exception exception, int expected)
    {
        var (status, body) = await HandleAsync(exception);

        Assert.Equal(expected, status);
        Assert.Equal(expected, body.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task ConcurrencyConflict_HidesEfInternalsFromClient()
    {
        var (_, body) = await HandleAsync(new DbUpdateConcurrencyException("expected to affect 1 row(s), but actually affected 0 row(s)"));

        var detail = body.GetProperty("detail").GetString();
        Assert.DoesNotContain("row(s)", detail);
        Assert.Contains("try again", detail);
    }

    [Fact]
    public async Task ServerError_HidesExceptionMessage()
    {
        var (_, body) = await HandleAsync(new InvalidOperationException("connection string Password=secret"));

        Assert.Equal("An unexpected error occurred.", body.GetProperty("detail").GetString());
    }
}
