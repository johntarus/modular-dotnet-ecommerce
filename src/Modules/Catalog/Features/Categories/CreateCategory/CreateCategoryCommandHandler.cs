using Catalog.Domain.Entities;
using Catalog.Infrastructure.Persistence;
using MediatR;

namespace Catalog.Features.Categories.CreateCategory;

public class CreateCategoryCommandHandler(CatalogDbContext _context) : IRequestHandler<CreateCategoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = Category.Create(
            request.Name,
            request.Description);
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return category.Id;
    }
}