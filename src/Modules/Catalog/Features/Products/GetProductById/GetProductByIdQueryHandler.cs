using Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Features.Products.GetProductById;

public class GetProductByIdQueryHandler(CatalogDbContext _context) : IRequestHandler<GetProductByIdQuery, ProductResponse>
{
    public async Task<ProductResponse?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .Select(p => new ProductResponse(
                p.Id,
                p.Name,
                p.Price,
                p.Description,
                p.CategoryId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}