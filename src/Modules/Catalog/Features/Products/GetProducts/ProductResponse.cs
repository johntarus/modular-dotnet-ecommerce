namespace Catalog.Features.Products.GetProducts;

public record ProductResponse(
    Guid Id,
    string Name,
    decimal Price,
    string Description,
    Guid CategoryId);