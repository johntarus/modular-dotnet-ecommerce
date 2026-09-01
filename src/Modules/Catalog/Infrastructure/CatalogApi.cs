using Catalog.Contracts;
using Catalog.Contracts.Dtos;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

// Internal implementation — registered in DI as ICatalogApi.
// Other modules inject ICatalogApi and never know this class exists.
internal sealed class CatalogApi(CatalogDbContext db) : ICatalogApi
{
    public async Task<ProductDto?> GetProductByIdAsync(Guid productId, CancellationToken ct = default)
    {
        var product = await db.Products
            .AsNoTracking()
            .Where(p => p.Id == productId && !p.IsDeleted)
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.CategoryId,
                p.CreatedAt,
                p.UpdatedAt))
            .FirstOrDefaultAsync(ct);

        return product;
    }

    public async Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct = default)
    {
        return await db.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == productId && !p.IsDeleted, ct);
    }
}

