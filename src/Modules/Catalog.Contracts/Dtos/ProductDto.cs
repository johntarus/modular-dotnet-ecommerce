namespace Catalog.Contracts.Dtos;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Sku,
    decimal Price,
    string Currency,
    bool IsActive
);