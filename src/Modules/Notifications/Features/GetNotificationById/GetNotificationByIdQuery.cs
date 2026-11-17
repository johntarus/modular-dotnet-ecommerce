using MediatR;

namespace Notifications.Features.GetNotificationById;

public sealed record GetNotificationByIdQuery(Guid Id) : IRequest<NotificationDetailsResponse>;