namespace Inventory.Contracts;

public interface IInventoryModuleApi
{
    Task<bool> IsStockAvailableAsync(Guid productId, int quantity, CancellationToken ct = default);
}