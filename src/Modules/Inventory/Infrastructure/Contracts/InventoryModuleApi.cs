using Inventory.Contracts;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Contracts;

// Internal implementation — registered in DI as IInventoryModuleApi.
internal sealed class InventoryModuleApi(InventoryDbContext dbContext) : IInventoryModuleApi
{
    public async Task<bool> IsStockAvailableAsync(Guid productId, int quantity, CancellationToken ct)
    {
        var item = await dbContext.InventoryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ProductId == productId, ct);

        // If item doesn't exist, or available quantity is less than requested, return false
        return item is not null && item.AvailableQuantity >= quantity;
    }
}