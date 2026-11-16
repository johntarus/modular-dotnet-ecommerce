using Customers.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Notifications.Domain.Entities;
using Notifications.Features.Email;
using Notifications.Infrastructure.Email;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Features.EventHandlers;

internal class CustomerRegisteredConsumer(
    NotificationsDbContext db, 
    IEmailSender emailSender,
    ILogger<CustomerRegisteredConsumer> logger) 
    : IConsumer<CustomerRegisteredIntegrationEvent>
{
    public async Task Consume(ConsumeContext<CustomerRegisteredIntegrationEvent> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        logger.LogInformation("Processing welcome email for {Email}", message.Email);

        var subject = "Welcome to our store!";
        var body = $@"
            <h1>Welcome, {message.FirstName}!</h1>
            <p>We are thrilled to have you. Start shopping today!</p>
        ";

        // Create the log
        var log = NotificationLog.Create(message.Email, subject, body);
        db.NotificationLogs.Add(log);
        await db.SaveChangesAsync(ct);

        try
        {
            var email = new EmailMessage(
                message.Email,
                subject,
                body);
            // Send the email
            await emailSender.SendEmailAsync(email, ct);

            // Mark as sent
            log.MarkAsSent();
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email}", message.Email);
            log.MarkAsFailed();
            await db.SaveChangesAsync(ct);
            
            // Throwing ensures MassTransit retries the message or puts it in the _error queue
            throw; 
        }
    }
}