namespace Catalog.Domain.Events;

public record ProductCreatedDomainEvent(
    Guid ProductId);