using BuildingBlocks.EventBus;
using Customers.Contracts.Events;
using Customers.Domain.Events;
using MediatR;

namespace Customers.Features.Customers.EventHandlers;

internal sealed class CustomerRegisteredDomainEventHandler : INotificationHandler<CustomerRegisteredDomainEvent>
{
    private readonly IEventBus _eventBus;

    public CustomerRegisteredDomainEventHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task Handle(CustomerRegisteredDomainEvent notification, CancellationToken ct)
    {
        var integrationEvent = new CustomerRegisteredIntegrationEvent(
            notification.CustomerId,
            notification.Email,
            notification.FirstName,
            DateTime.UtcNow);

        await _eventBus.PublishAsync(integrationEvent, ct);
    }
}