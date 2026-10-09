using MediatR;

namespace Catalog.Features.Products.GetProductById;

public sealed record GetProductByIdQuery(
    Guid Id) : IRequest<ProductResponse>;