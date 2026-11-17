using BuildingBlocks.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Features.GetNotificationById;

internal sealed class GetNotificationByIdQueryHandler(NotificationsDbContext context)
    : IRequestHandler<GetNotificationByIdQuery, NotificationDetailsResponse>
{
    public async Task<NotificationDetailsResponse> Handle(GetNotificationByIdQuery request, CancellationToken ct)
    {
        var log = await context.NotificationLogs
                      .AsNoTracking()
                      .FirstOrDefaultAsync(l => l.Id == request.Id, ct)
                  ?? throw new NotFoundException("Notification log not found.");

        return new NotificationDetailsResponse(
            log.Id,
            log.Recipient,
            log.Subject,
            log.Body,
            log.Status,
            log.ErrorMessage,
            log.CreatedAt,
            log.SentAt
        );
    }
}