using Catalog.Contracts.Events;
using Catalog.Domain.Events;
using MediatR;

namespace Catalog.Features.Products.EventHandlers;

public class ProductUpdatedDomainEventHandler : INotificationHandler<ProductUpdatedDomainEvent>
{
    public Task Handle(ProductUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new ProductUpdatedIntegrationEvent(
            notification.ProductId,
            notification.Name,
            notification.Description,
            notification.CategoryId,
            notification.OccurredAt);

        // TODO: Publish integrationEvent to event bus
        
        return Task.CompletedTask;
    }
}
