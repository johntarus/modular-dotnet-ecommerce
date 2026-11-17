using Notifications.Domain.Enums;

namespace Notifications.Domain.Entities;

public class NotificationLog
{
    public Guid Id { get; private set; }
    public string Recipient { get; private set; }
    public string Subject { get; private set; }
    public string Body { get; private set; }
    public NotificationStatus Status { get; private set; } // Pending, Sent, Failed
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }

    private NotificationLog() { }

    public static NotificationLog Create(string recipient, string subject, string body)
    {
        return new NotificationLog
        {
            Id = Guid.NewGuid(),
            Recipient = recipient,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = NotificationStatus.Failed;
        ErrorMessage = errorMessage;
        SentAt = DateTime.UtcNow;
    }
}