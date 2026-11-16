using BuildingBlocks.Contracts.Events;

namespace Customers.Contracts.Events;

public sealed record CustomerRegisteredIntegrationEvent(
    Guid CustomerId,
    string Email,
    string FirstName,
    DateTime RegisteredAt
) : IIntegrationEvent
{
    public Guid EventId { get; }
    public DateTime OccurredAt { get; }
}