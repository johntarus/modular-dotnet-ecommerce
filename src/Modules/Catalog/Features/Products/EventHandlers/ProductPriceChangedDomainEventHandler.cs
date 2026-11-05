using BuildingBlocks.EventBus;
using Catalog.Contracts.Events;
using Catalog.Domain.Events;
using MediatR;

namespace Catalog.Features.Products.EventHandlers;

public class ProductPriceChangedDomainEventHandler : INotificationHandler<ProductPriceChangedDomainEvent>
{
    private readonly IEventBus _eventBus;

    public ProductPriceChangedDomainEventHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task Handle(ProductPriceChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new ProductPriceChangedIntegrationEvent(
            notification.ProductId,
            notification.OldPrice,
            notification.NewPrice,
            notification.OccurredAt);

        // Awaited so the message is written to the outbox before CatalogDbContext saves (same transaction)
        await _eventBus.PublishAsync(integrationEvent, cancellationToken);
    }
}
