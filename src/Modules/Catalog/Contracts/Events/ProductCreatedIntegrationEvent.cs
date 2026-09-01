namespace Catalog.Contracts.Events;

public sealed record ProductCreatedIntegrationEvent(
    Guid ProductId,
    string Name,
    decimal Price,
    Guid CategoryId,
    DateTime OccurredAt
);