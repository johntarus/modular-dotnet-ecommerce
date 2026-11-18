using BuildingBlocks.Contracts.Events;

namespace Catalog.Contracts.Events;

public sealed record ProductUpdatedIntegrationEvent(
    Guid ProductId,
    string Name,
    string Description,
    Guid CategoryId,
    DateTime OccurredAt) : IIntegrationEvent
{
    // init (not get-only) so the publisher's id survives JSON deserialization on the consumer side
    public Guid EventId { get; init; } = Guid.NewGuid();
}
