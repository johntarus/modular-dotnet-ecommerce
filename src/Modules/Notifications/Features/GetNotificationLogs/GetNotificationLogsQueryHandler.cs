using MediatR;
using Microsoft.EntityFrameworkCore;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Features.GetNotificationLogs;

internal sealed class GetNotificationLogsQueryHandler(NotificationsDbContext context) : IRequestHandler<GetNotificationLogsQuery, List<NotificationLogResponse>>
{
    public async Task<List<NotificationLogResponse>> Handle(GetNotificationLogsQuery request, CancellationToken ct)
    {
        return await context.NotificationLogs
            .AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Take(request.Limit)
            .Select(l => new NotificationLogResponse(
                l.Id,
                l.Recipient,
                l.Subject,
                l.Status,
                l.CreatedAt,
                l.SentAt
            ))
            .ToListAsync(ct);
    }
}