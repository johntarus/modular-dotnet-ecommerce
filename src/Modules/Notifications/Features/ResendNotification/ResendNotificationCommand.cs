using MediatR;

namespace Notifications.Features.ResendNotification;

public sealed record ResendNotificationCommand(Guid LogId) : IRequest;