using Catalog.Contracts;
using Catalog.Contracts.Dtos;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

// Internal implementation — registered in DI as ICatalogApi.
// Other modules inject ICatalogApi and never know this class exists.
internal sealed class CatalogApi(CatalogDbContext context) : ICatalogApi
{
    public async Task<ProductDto?> GetProductByIdAsync(Guid productId, CancellationToken ct = default)
    {
        var product = await context.Products
            .AsNoTracking()
            .Where(p => p.Id == productId && !p.IsDeleted)
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Sku,
                p.Price,
                p.Currency,
                !p.IsDeleted))
            .FirstOrDefaultAsync(ct);

        return product;
    }
    
    public async Task<IReadOnlyList<ProductDto>> GetProductsByIdAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct = default)
    {
        return await context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Sku,
                p.Price,
                p.Currency,
                !p.IsDeleted))
            .ToListAsync(ct);

    }

    public async Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct = default)
    {
        return await context.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == productId && !p.IsDeleted, ct);
    }
}

