using Notifications.Domain.Enums;

namespace Notifications.Features.GetNotificationLogs;

public sealed record NotificationLogResponse(Guid Id,
    string Recipient,
    string Subject,
    NotificationStatus Status,
    DateTime CreatedAtUtc,
    DateTime? SentAtUtc);