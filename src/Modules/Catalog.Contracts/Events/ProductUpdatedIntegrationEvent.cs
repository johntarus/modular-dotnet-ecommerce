using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Domain;

namespace Catalog.Contracts.Events;

public sealed record ProductUpdatedIntegrationEvent(
    Guid ProductId,
    string Name,
    string Description,
    Guid CategoryId,
    DateTime OccurredAt) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
