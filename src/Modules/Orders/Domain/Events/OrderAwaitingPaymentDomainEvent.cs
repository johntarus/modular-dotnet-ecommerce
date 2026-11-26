using BuildingBlocks.Domain;

namespace Orders.Domain.Events;

public sealed record OrderAwaitingPaymentDomainEvent
(
    Guid orderId,
    Guid customerId) : IDomainEvent
{
    public Guid EventId { get; }
    public DateTime OccurredAt { get; }
}