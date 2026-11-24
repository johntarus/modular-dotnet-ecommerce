using Catalog.Contracts.Events;
using Catalog.Domain.Events;
using MassTransit;
using MediatR;

namespace Catalog.Features.Products.EventHandlers;

public class ProductCreatedKafkaEventHandler(ITopicProducer<ProductCreatedIntegrationEvent> producer)
    : INotificationHandler<ProductCreatedDomainEvent>
{
    public async Task Handle(ProductCreatedDomainEvent notification, CancellationToken ct)
    {
        var integrationEvent = new ProductCreatedIntegrationEvent(
            notification.ProductId, notification.Name, notification.Price, notification.CategoryId,
            DateTime.UtcNow);

        // THIS publishes directly to Kafka via the Rider
        await producer.Produce(integrationEvent);
    }
}