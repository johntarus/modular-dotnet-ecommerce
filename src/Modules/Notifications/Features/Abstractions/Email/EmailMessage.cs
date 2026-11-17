namespace Notifications.Features.Abstractions.Email;

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null
);