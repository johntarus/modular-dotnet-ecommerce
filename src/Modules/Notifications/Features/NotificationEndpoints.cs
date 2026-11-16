using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Features;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications");

        // 1. Admin Endpoint: View recent logs
        group.MapGet("/logs", async (NotificationsDbContext db) =>
        {
            var logs = await db.NotificationLogs
                .OrderByDescending(l => l.CreatedAtUtc)
                .Take(50)
                .ToListAsync();
                
            return Results.Ok(logs);
        }).RequireAuthorization("Admin"); // Only Admins can view logs

        // 2. Webhook Endpoint (Example: SendGrid)
        // SendGrid will hit this when an email bounces
        group.MapPost("/webhooks/email", async (HttpContext context) =>
        {
            // In a real app, you would read the webhook payload and update the NotificationLog status
            // var payload = await JsonSerializer.DeserializeAsync<SendGridWebhookPayload>(context.Request.Body);
            return Results.Ok();
        }).AllowAnonymous(); // Webhooks usually come from external servers, so they don't have your JWT
    }
}