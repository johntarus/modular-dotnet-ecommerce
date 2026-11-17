using MediatR;

namespace Notifications.Features.GetNotificationLogs;

public sealed record GetNotificationLogsQuery(int Limit = 50) : IRequest<List<NotificationLogResponse>>;