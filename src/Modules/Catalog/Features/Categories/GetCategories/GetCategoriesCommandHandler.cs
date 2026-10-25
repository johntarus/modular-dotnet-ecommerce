using BuildingBlocks.Shared;
using BuildingBlocks.Shared.Pagination;
using Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Features.Categories.GetCategories;

internal class GetCategoriesCommandHandler(CatalogDbContext _context)
    : IRequestHandler<GetCategoriesQuery, PagedResult<CategoryResponse>>
{
    public async Task<PagedResult<CategoryResponse>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Description));
        return await query.ToPagedResultAsync(new PaginationRequest(request.Page, request.PageSize), cancellationToken);
    }
}