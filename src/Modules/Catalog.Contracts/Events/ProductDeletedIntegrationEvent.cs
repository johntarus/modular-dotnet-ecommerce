using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Domain;

namespace Catalog.Contracts.Events;

public sealed record ProductDeletedIntegrationEvent(
    Guid ProductId,
    DateTime OccurredAt) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
