using BuildingBlocks.Contracts.Events;

namespace BuildingBlocks.EventBus;

public interface IEventBus
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : IIntegrationEvent;
}