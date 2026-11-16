using Customers.Contracts.Events;
using Customers.Domain.Events;
using MassTransit;
using MediatR;

namespace Customers.Features.Customers.EventHandlers;

// Uses IBus rather than IEventBus/IPublishEndpoint: the scoped publish endpoint is bound to Catalog's bus outbox,
// so a publish through it would sit in an unsaved CatalogDbContext and never reach RabbitMQ.
// CustomersDbContext dispatches this after the customer row is committed.
internal sealed class CustomerRegisteredDomainEventHandler(IBus bus) : INotificationHandler<CustomerRegisteredDomainEvent>
{
    public async Task Handle(CustomerRegisteredDomainEvent notification, CancellationToken ct)
    {
        var integrationEvent = new CustomerRegisteredIntegrationEvent(
            notification.CustomerId,
            notification.Email,
            notification.FirstName,
            DateTime.UtcNow);

        await bus.Publish(integrationEvent, ct);
    }
}
