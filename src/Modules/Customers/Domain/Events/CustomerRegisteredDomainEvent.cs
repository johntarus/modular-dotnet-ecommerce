using BuildingBlocks.Domain;
using MediatR;

namespace Customers.Domain.Events;

public sealed record CustomerRegisteredDomainEvent(
    Guid CustomerId,
    string? Email,      // the token may not carry an email claim
    string? FirstName) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}