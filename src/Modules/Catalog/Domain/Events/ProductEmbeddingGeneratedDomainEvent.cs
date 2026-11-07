using BuildingBlocks.Domain;

namespace Catalog.Domain.Events;

public sealed record ProductEmbeddingGeneratedDomainEvent(
    Guid ProductId,
    DateTime GeneratedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}