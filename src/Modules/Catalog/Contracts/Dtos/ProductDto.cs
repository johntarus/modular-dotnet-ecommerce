namespace Catalog.Contracts.Dtos;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    Guid CategoryId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);