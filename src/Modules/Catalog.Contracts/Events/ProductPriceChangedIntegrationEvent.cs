using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Domain;

namespace Catalog.Contracts.Events;

public sealed record ProductPriceChangedIntegrationEvent(
    Guid ProductId,
    decimal OldPrice,
    decimal NewPrice,
    DateTime OccurredAt) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}