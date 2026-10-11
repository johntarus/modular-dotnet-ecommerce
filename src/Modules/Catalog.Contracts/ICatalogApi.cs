using Catalog.Contracts.Dtos;

namespace Catalog.Contracts;

public interface ICatalogApi
{
    Task<ProductDto?> GetProductByIdAsync(Guid productId, CancellationToken ct = default);
    Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct = default);
}