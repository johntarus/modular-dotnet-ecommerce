using Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Features.Categories.UpdateCategory;

public class UpdateCategoryCommandHandler(
    CatalogDbContext _context
) : IRequestHandler<UpdateCategoryCommand>
{
    public async Task Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(
                x => x.Id == request.Id && !x.IsDeleted,
                cancellationToken);

        if (category is null)
        {
            throw new KeyNotFoundException(
                $"Category with ID '{request.Id}' was not found.");
        }

        category.UpdateDetails(
            request.Name,
            request.Description);

        await _context.SaveChangesAsync(cancellationToken);
    }
}