using MediatR;

namespace Catalog.Features.Products.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : IRequest;
