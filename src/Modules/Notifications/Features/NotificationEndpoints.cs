using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Notifications.Features.GetNotificationById;
using Notifications.Features.GetNotificationLogs;
using Notifications.Features.ProcessEmailWebhook;
using Notifications.Features.ResendNotification;
using Notifications.Features.UpdateNotificationPreferences;

namespace Notifications.Features;

public static class NotificationEndpoints
{
    public static RouteGroupBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications");

        // Admin Endpoints
        var adminGroup = group.MapGroup("/");
        adminGroup.MapGet("/logs", GetLogs);
        adminGroup.MapGet("/logs/{id:guid}", GetNotificationById);
        adminGroup.MapPost("/logs/{id:guid}/resend", ResendNotification);

        // Customer Endpoints
        group.MapPut("/preferences", UpdatePreferences).RequireAuthorization();

        // Public Webhooks
        group.MapPost("/webhooks/email", ProcessEmailWebhook).AllowAnonymous();

        return group;
    }

    private static async Task<IResult> GetLogs(ISender sender, CancellationToken ct)
    {
        var logs = await sender.Send(new GetNotificationLogsQuery(), ct);
        return Results.Ok(logs);
    }

    private static async Task<IResult> GetNotificationById(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetNotificationByIdQuery(id), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> ResendNotification(Guid id, ISender sender, CancellationToken ct)
    {
        await sender.Send(new ResendNotificationCommand(id), ct);
        return Results.Ok(new { Message = "Notification resent successfully." });
    }

    private static async Task<IResult> UpdatePreferences(UpdateNotificationPreferencesCommand command, ISender sender, CancellationToken ct)
    {
        await sender.Send(command, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ProcessEmailWebhook(HttpContext context, ISender sender, CancellationToken ct)
    {
        using var reader = new StreamReader(context.Request.Body);
        var payload = await reader.ReadToEndAsync(ct);

        await sender.Send(new ProcessEmailWebhookCommand(payload), ct);
        
        return Results.Ok();
    }
}