using BuildingBlocks.Domain;
using MediatR;

namespace Customers.Domain.Events;

public sealed record CustomerRegisteredDomainEvent(
    Guid CustomerId,
    string Email,
    string FirstName) : IDomainEvent
{
    public Guid EventId { get; }
    public DateTime OccurredAt { get; }
}