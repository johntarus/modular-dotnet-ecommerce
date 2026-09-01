using MediatR;

namespace Catalog.Features.Products.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    Guid CategoryId
) : IRequest<Guid>;

