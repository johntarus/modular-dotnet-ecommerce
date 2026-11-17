using Notifications.Domain.Enums;

namespace Notifications.Features.GetNotificationById;

public sealed record NotificationDetailsResponse(
    Guid Id,
    string Recipient,
    string Subject,
    string Body,
    NotificationStatus Status,
    string? ErrorMessage,
    DateTime CreatedAtUtc,
    DateTime? SentAtUtc
);

