using BuildingBlocks.Contracts.Events;

namespace Customers.Contracts.Events;

public sealed record CustomerRegisteredIntegrationEvent(
    Guid CustomerId,
    string? Email,      // null when the identity provider didn't supply one; consumers must handle it
    string? FirstName,
    DateTime RegisteredAt
) : IIntegrationEvent
{
    // init (not get-only) so the publisher's values survive JSON deserialization on the consumer side
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}