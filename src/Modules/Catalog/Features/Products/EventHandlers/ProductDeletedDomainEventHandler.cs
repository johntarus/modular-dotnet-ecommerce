using Catalog.Contracts.Events;
using Catalog.Domain.Events;
using MediatR;

namespace Catalog.Features.Products.EventHandlers;

public class ProductDeletedDomainEventHandler : INotificationHandler<ProductDeletedDomainEvent>
{
    public Task Handle(ProductDeletedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new ProductDeletedIntegrationEvent(
            notification.ProductId,
            notification.OccurredAt);

        // TODO: Publish integrationEvent to event bus
        
        return Task.CompletedTask;
    }
}
