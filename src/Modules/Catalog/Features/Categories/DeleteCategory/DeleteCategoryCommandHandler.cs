using Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Features.Categories.DeleteCategory;

public class DeleteCategoryCommandHandler(CatalogDbContext _context) : IRequestHandler<DeleteCategoryCommand>
{
    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (category is null) throw new KeyNotFoundException($"Category {request.Id} was not found");
        category.Delete();
        await _context.SaveChangesAsync(cancellationToken);
    }
}