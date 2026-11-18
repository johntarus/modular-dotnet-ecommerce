using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Domain;

namespace Catalog.Contracts.Events;

public sealed record ProductDeletedIntegrationEvent(
    Guid ProductId,
    DateTime OccurredAt) : IIntegrationEvent
{
    // init (not get-only) so the publisher's id survives JSON deserialization on the consumer side
    public Guid EventId { get; init; } = Guid.NewGuid();
}
