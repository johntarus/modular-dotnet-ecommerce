using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace BuildingBlocks.Infrastructure.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        var correlationId =
            context.Request.Headers[HeaderName]
                .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId =
                Guid.NewGuid().ToString("N");
        }

        context.Items[HeaderName] =
            correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[
                HeaderName] = correlationId;

            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(
                   "CorrelationId",
                   correlationId))
        {
            await _next(context);
        }
    }
}