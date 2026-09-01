using MediatR;

namespace Catalog.Features.Products.GetProducts;

public sealed record GetProductsQuery(
    int Page = 1,
    int PageSize = 10
) : IRequest<List<ProductResponse>>;