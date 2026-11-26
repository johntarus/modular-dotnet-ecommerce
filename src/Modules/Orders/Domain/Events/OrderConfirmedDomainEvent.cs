using BuildingBlocks.Domain;

namespace Orders.Domain.Events;

public sealed record OrderConfirmedDomainEvent(
    Guid OrderId) : IDomainEvent
{
    public Guid EventId { get; }
    public DateTime OccurredAt { get; }
}
