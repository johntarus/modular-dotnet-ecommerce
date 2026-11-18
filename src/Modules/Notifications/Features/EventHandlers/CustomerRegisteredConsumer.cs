using Customers.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Notifications.Domain.Entities;
using Notifications.Features.Abstractions.Email;
using Notifications.Features.Abstractions.Email.Templates.WelcomeEmail;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Features.EventHandlers;

internal class CustomerRegisteredConsumer(
    NotificationsDbContext _context, 
    IEmailSender emailSender,
    IEmailTemplate<WelcomeEmailModel> welcomeTemplate,
    ILogger<CustomerRegisteredConsumer> logger) 
    : IConsumer<CustomerRegisteredIntegrationEvent>
{
    public async Task Consume(ConsumeContext<CustomerRegisteredIntegrationEvent> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        // Nowhere to send it. Not an error: retrying can't help, so don't throw (that would fault the message).
        if (string.IsNullOrWhiteSpace(message.Email))
        {
            logger.LogWarning("Skipping welcome email for customer {CustomerId}: no email address on their account",
                message.CustomerId);
            return;
        }

        logger.LogInformation("Processing welcome email for {Email}", message.Email);

        var model = new WelcomeEmailModel(message.Email,
            string.IsNullOrWhiteSpace(message.FirstName) ? "Valued Customer" : message.FirstName);
        
        var email =  welcomeTemplate.Generate(model);

        // Create the log
        var log = NotificationLog.Create(email.To, email.Subject, email.HtmlBody);
        _context.NotificationLogs.Add(log);
        await _context.SaveChangesAsync(ct);

        try
        {
            // Send the email
            await emailSender.SendEmailAsync(email, ct);

            // Mark as sent
            log.MarkAsSent();
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email}", message.Email);
            log.MarkAsFailed(ex.Message);
            await _context.SaveChangesAsync(ct);
            
            // Throwing ensures MassTransit retries the message or puts it in the _error queue
            throw; 
        }
    }
}