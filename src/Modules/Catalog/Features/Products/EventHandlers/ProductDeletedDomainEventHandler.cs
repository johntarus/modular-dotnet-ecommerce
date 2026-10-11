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

    public Task Handle(ProductDeletedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new ProductDeletedIntegrationEvent(
            notification.ProductId,
            notification.OccurredAt);

        //: Publish integrationEvent to event bus
        _eventBus.PublishAsync(integrationEvent, cancellationToken);
        
        return Task.CompletedTask;
    }
}
