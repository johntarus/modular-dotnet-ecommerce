using BuildingBlocks.Shared;
using Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Features.Products.GetProducts;

internal class GetProductsQueryHandler(
    CatalogDbContext _context
) : IRequestHandler<GetProductsQuery, PagedResult<ProductResponse>>
{
    public async Task<PagedResult<ProductResponse>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProductResponse(
                p.Id,
                p.Name,
                p.Price,
                p.Description,
                p.CategoryId
            ));
        return await query.ToPagedResultAsync(new PaginationRequest(request.Page, request.PageSize), cancellationToken);
    }
}