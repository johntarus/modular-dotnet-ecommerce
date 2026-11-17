using MediatR;

namespace Notifications.Features.UpdateNotificationPreferences;

public sealed record UpdateNotificationPreferencesCommand(bool Marketing, bool OrderUpdates) : IRequest;