using Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Features.Categories.GetCategoryById;

public class GetCategoryByIdQueryHandler(CatalogDbContext _context)
    : IRequestHandler<GetCategoryByIdQuery, CategoryResponse>
{
    public async Task<CategoryResponse> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .Where(c=>c.Id == request.Id)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Description))
            .FirstOrDefaultAsync(cancellationToken);

        if (category == null)
            throw new KeyNotFoundException($"Category with the id'{request.Id}' was not ffound");
        return category;
    }
}