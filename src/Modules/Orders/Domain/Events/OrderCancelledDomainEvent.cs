using BuildingBlocks.Domain;

namespace Orders.Domain.Events;

public sealed record OrderCancelledDomainEvent(Guid OrderId, Guid CustomerId, string Reason) : IDomainEvent
{
    public Guid Id { get; }
    public Guid CustomerId { get; }
    public string Reason { get; }
    public Guid EventId { get; }
    public DateTime OccurredAt { get; }
}