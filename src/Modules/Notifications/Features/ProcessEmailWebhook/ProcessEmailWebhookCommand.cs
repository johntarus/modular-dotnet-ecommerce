using MediatR;

namespace Notifications.Features.ProcessEmailWebhook;

public sealed record ProcessEmailWebhookCommand(string RawPayload) : IRequest;
