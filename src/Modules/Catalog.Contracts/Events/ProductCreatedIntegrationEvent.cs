using BuildingBlocks.Contracts.Events;

namespace Catalog.Contracts.Events;

public sealed record ProductCreatedIntegrationEvent(
    Guid ProductId,
    string Name,
    decimal Price,
    Guid CategoryId,
    DateTime OccurredAt) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}