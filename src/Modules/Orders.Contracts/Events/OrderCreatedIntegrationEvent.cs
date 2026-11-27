using BuildingBlocks.Contracts.Events;

namespace Orders.Contracts.Events;

public sealed record OrderCreatedIntegrationEvent(
    Guid EventId,
    Guid OrderId,
    Guid CustomerId,
    DateTime utcNow) : IIntegrationEvent
{
    public DateTime OccurredAt { get; }
}