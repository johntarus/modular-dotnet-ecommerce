using BuildingBlocks.EventBus;
using Catalog.Contracts.Events;
using Catalog.Domain.Events;
using MediatR;

namespace Catalog.Features.Products.EventHandlers;

public class ProductCreatedDomainEventHandler
    : INotificationHandler<ProductCreatedDomainEvent>
{
    private readonly IEventBus _eventBus;

    public ProductCreatedDomainEventHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    //  Publish integration event via event bus
    public async Task Handle(
        ProductCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        //  For now, just log or publish to integration event bus
        var integrationEvent = new ProductCreatedIntegrationEvent(
            notification.ProductId,
            notification.Name,
            notification.Price,
            notification.CategoryId,
            notification.OccurredAt);

        // Publish integrationEvent to event bus (RabbitMQ, etc.)
        await _eventBus.PublishAsync(integrationEvent, cancellationToken);
    }
}