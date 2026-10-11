using BuildingBlocks.Exceptions;
using Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Features.Products.GetProductById;

internal class GetProductByIdQueryHandler(CatalogDbContext _context) : IRequestHandler<GetProductByIdQuery, ProductResponse>
{
    public async Task<ProductResponse?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .Select(p => new ProductResponse(
                p.Id,
                p.Name,
                p.Price,
                p.Description,
                p.CategoryId))
            .FirstOrDefaultAsync(cancellationToken);
        if (product is null)
            throw new NotFoundException($"Product with the id '{request.Id}' was not found");
        return product;
    }
}