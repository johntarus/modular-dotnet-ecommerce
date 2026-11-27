using MassTransit;
using MediatR;
using Orders.Contracts.Events;
using Orders.Domain.Events;

namespace Orders.Features.EventHandlers;

internal sealed class OrderCreatedDomainEventHandler(
    IPublishEndpoint publishEndpoint
) : INotificationHandler<OrderCreatedDomainEvent>
{
    public async Task Handle(
        OrderCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new OrderCreatedIntegrationEvent(
            Guid.NewGuid(),
            notification.OrderId,
            notification.CustomerId,
            DateTime.UtcNow
        );

        await publishEndpoint.Publish(
            integrationEvent,
            cancellationToken);
    }
}