using MailKit;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Notifications.Features.Email;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Notifications.Infrastructure.Email;

internal sealed class SmtpEmailSender(
    IOptions<EmailOptions> options
) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendEmailAsync(
        EmailMessage message,
        CancellationToken ct = default)
    {
        var email = new MimeMessage();

        email.From.Add(new MailboxAddress(
            _options.SenderName,
            _options.SenderEmail
        ));

        email.To.Add(MailboxAddress.Parse(message.To));
        email.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.PlainTextBody
        };

        email.Body = bodyBuilder.ToMessageBody();

        using var smtpClient = new SmtpClient();

        await smtpClient.ConnectAsync(
            _options.Host,
            _options.Port,
            SecureSocketOptions.StartTls,
            ct
        );

        await smtpClient.AuthenticateAsync(
            _options.Username,
            _options.Password,
            ct
        );

        await smtpClient.SendAsync(email, ct);

        await smtpClient.DisconnectAsync(true, ct);
    }
}