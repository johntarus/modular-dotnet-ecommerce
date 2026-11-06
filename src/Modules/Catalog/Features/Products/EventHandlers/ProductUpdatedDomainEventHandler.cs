using BuildingBlocks.EventBus;
using Catalog.Contracts.Events;
using Catalog.Domain.Events;
using MediatR;

namespace Catalog.Features.Products.EventHandlers;

public class ProductUpdatedDomainEventHandler(IEventBus eventBus) : INotificationHandler<ProductUpdatedDomainEvent>
{
    public async Task Handle(ProductUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new ProductUpdatedIntegrationEvent(
            notification.ProductId,
            notification.Name,
            notification.Description,
            notification.CategoryId,
            notification.OccurredAt);

        // Awaited so the message is written to the outbox before CatalogDbContext saves (same transaction)
        await eventBus.PublishAsync(integrationEvent, cancellationToken);
    }
}
