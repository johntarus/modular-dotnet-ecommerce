using BuildingBlocks.Contracts.Events;
using BuildingBlocks.EventBus;
using MassTransit;

namespace Catalog.Infrastructure.Messaging;

public class EventBus : IEventBus
{
    private readonly IPublishEndpoint _publishEndpoint;

    public EventBus(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }
    public async Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : IIntegrationEvent
    {
        await _publishEndpoint.Publish(integrationEvent, ct);
    }
}