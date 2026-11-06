using BuildingBlocks.Domain;

namespace Inventory.Domain.Events;

public sealed record StockCreatedDomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
