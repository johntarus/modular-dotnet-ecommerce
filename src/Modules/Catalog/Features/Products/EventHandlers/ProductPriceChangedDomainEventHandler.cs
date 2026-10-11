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

    public Task Handle(ProductPriceChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new ProductPriceChangedIntegrationEvent(
            notification.ProductId,
            notification.OldPrice,
            notification.NewPrice,
            notification.OccurredAt);

        //  Publish integrationEvent to event bus
        _eventBus.PublishAsync(integrationEvent, cancellationToken);
        
        return Task.CompletedTask;
    }
}
