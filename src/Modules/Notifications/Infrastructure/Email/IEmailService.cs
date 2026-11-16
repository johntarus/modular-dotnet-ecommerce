using Notifications.Features.Email;

namespace Notifications.Infrastructure.Email;

public interface IEmailService
{
    Task SendEmailAsync(EmailMessage message, CancellationToken ct = default);
}