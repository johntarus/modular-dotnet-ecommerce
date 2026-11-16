using Notifications.Domain.Enums;

namespace Notifications.Domain.Entities;

public class NotificationLog
{
    public Guid Id { get; private set; }
    public string Recipient { get; private set; }
    public string Subject { get; private set; }
    public string Body { get; private set; }
    public NotificationStatus Status { get; private set; } // Pending, Sent, Failed
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }

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
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void MarkAsSent()
    {
        Status = NotificationStatus.Sent;
        SentAtUtc = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        Status = NotificationStatus.Failed;
        SentAtUtc = DateTime.UtcNow;
    }
}