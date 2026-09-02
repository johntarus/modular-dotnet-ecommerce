using Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Features.Categories.GetCategories;

public class GetCategoriesCommandHandler(CatalogDbContext _context)
    : IRequestHandler<GetCategoriesQuery, List<CategoryResponse>>
{
    public async Task<List<CategoryResponse>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Description))
            .ToListAsync(cancellationToken);
    }
}