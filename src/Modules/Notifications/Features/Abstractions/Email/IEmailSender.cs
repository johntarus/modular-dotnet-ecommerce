namespace Notifications.Features.Abstractions.Email;

public interface IEmailSender
{
    Task SendEmailAsync(
        EmailMessage message,
        CancellationToken ct = default
    );
}