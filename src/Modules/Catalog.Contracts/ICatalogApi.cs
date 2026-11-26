using Catalog.Contracts.Dtos;

namespace Catalog.Contracts;

public interface ICatalogApi
{
    Task<ProductDto?> GetProductByIdAsync(Guid productId, CancellationToken ct = default);

    Task<IReadOnlyList<ProductDto>> GetProductsByIdAsync(IReadOnlyCollection<Guid> productIds,
        CancellationToken ct = default);

    Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct = default);
}