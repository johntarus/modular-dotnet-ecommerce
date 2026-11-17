using BuildingBlocks.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notifications.Features.Abstractions.Email;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Features.ResendNotification;

internal sealed class ResendNotificationCommandHandler(
    NotificationsDbContext context,
    IEmailSender emailSender,
    ILogger<ResendNotificationCommandHandler> logger)
    : IRequestHandler<ResendNotificationCommand>
{
    public async Task Handle(ResendNotificationCommand request, CancellationToken ct)
    {
        var log = await context.NotificationLogs
                      .FirstOrDefaultAsync(l => l.Id == request.LogId, ct)
                  ?? throw new NotFoundException("Notification log not found.");

        try
        {
            // Attempt to resend using the exact same body and subject
            var email = new EmailMessage(log.Recipient, log.Subject, log.Body);
            await emailSender.SendEmailAsync(email, ct);
            
            // Update status on success
            log.MarkAsSent();
            await context.SaveChangesAsync(ct);
            
            logger.LogInformation("Successfully resent notification {Id} to {Recipient}", log.Id, log.Recipient);
        }
        catch (Exception ex)
        {
            log.MarkAsFailed(ex.Message);
            await context.SaveChangesAsync(ct);
            
            logger.LogError(ex, "Failed to resend notification {Id}", log.Id);
            throw; // Rethrow so the API returns a 500 error to the admin
        }
    }
}