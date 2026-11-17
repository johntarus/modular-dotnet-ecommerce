using Microsoft.Extensions.Logging;
using Notifications.Features.Abstractions.Email;

namespace Notifications.Infrastructure.Email;

public class FakeEmailSender(ILogger<FakeEmailSender> logger) : IEmailSender
{
    public Task SendEmailAsync(EmailMessage message, CancellationToken ct = default)
    {
        // In a prod, this would call SendGrid/AWS SES
        logger.LogInformation("📧 FAKE EMAIL SENT TO: {To} | SUBJECT: {Subject}", message.To, message.Subject);
        return Task.CompletedTask;
    }
}