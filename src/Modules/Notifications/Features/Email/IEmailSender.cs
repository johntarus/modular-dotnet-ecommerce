namespace Notifications.Features.Email;

public interface IEmailSender
{
    Task SendEmailAsync(
        EmailMessage message,
        CancellationToken ct = default
    );
}