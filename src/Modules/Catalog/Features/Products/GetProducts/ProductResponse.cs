namespace Catalog.Features.Products.GetProductById;

public record ProductResponse(
    Guid Id,
    string Name,
    decimal Price,
    string Description,
    Guid CategoryId);