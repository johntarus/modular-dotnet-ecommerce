using Microsoft.Extensions.Logging;

namespace Notifications.Features.ProcessEmailWebhook;

public class ProcessEmailWebhookCommandHandler(ILogger<ProcessEmailWebhookCommandHandler> logger)
{
    public Task Handle(ProcessEmailWebhookCommand request, CancellationToken ct)
    {
        // TODO: In the future, deserialize the payload (e.g., SendGrid or AWS SES format)
        // Find the NotificationLog by MessageId, and update its status to "Bounced" or "Opened".
        
        logger.LogInformation("Received email provider webhook: {Payload}", request.RawPayload);
        
        return Task.CompletedTask;
    }
}