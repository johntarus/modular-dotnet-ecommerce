using BuildingBlocks.Domain;

namespace Orders.Domain.Events;

public sealed record OrderCreatedDomainEvent(
    Guid OrderId,
    Guid CustomerId
):IDomainEvent
{
    public Guid EventId { get; }
    public DateTime OccurredAt { get; }
}