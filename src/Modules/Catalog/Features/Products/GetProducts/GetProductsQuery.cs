using BuildingBlocks.Shared;
using MediatR;

namespace Catalog.Features.Products.GetProducts;

public sealed record GetProductsQuery(
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<ProductResponse>>;