using BuildingBlocks.Exceptions;
using Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Features.Categories.DeleteCategory;

internal class DeleteCategoryCommandHandler(CatalogDbContext _context) : IRequestHandler<DeleteCategoryCommand>
{
    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (category is null) throw new NotFoundException($"Category {request.Id} was not found");

        // Deleted products are hidden by the query filter, so only active products block the delete
        var activeProducts = await _context.Products.CountAsync(p => p.CategoryId == request.Id, cancellationToken);
        if (activeProducts > 0)
            throw new ConflictException(
                $"Category {request.Id} still has {activeProducts} product(s). Move or delete them first.");

        category.Delete();
        await _context.SaveChangesAsync(cancellationToken);
    }
}