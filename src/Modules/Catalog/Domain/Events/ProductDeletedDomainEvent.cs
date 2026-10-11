using BuildingBlocks.Domain;

namespace Catalog.Domain.Events;

public sealed record ProductDeletedDomainEvent(
    Guid ProductId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
