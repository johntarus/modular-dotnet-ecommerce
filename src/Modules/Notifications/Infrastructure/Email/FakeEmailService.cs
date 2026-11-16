using Microsoft.Extensions.Logging;
using Notifications.Features.Email;

namespace Notifications.Infrastructure.Email;

public class FakeEmailService(ILogger<FakeEmailService> logger) : IEmailSender
{
    public Task SendEmailAsync(EmailMessage message, CancellationToken ct = default)
    {
        // In a real app, this would call SendGrid/AWS SES
        logger.LogInformation("📧 FAKE EMAIL SENT TO: {To} | SUBJECT: {Subject}", message.To, message.Subject);
        return Task.CompletedTask;
    }
}