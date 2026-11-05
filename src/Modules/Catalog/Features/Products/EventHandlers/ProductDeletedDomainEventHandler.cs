using BuildingBlocks.EventBus;
using Catalog.Contracts.Events;
using Catalog.Domain.Events;
using MediatR;

namespace Catalog.Features.Products.EventHandlers;

public class ProductDeletedDomainEventHandler : INotificationHandler<ProductDeletedDomainEvent>
{
    private readonly IEventBus _eventBus;

    public ProductDeletedDomainEventHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task Handle(ProductDeletedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new ProductDeletedIntegrationEvent(
            notification.ProductId,
            notification.OccurredAt);

        // Awaited so the message is written to the outbox before CatalogDbContext saves (same transaction)
        await _eventBus.PublishAsync(integrationEvent, cancellationToken);
    }
}
