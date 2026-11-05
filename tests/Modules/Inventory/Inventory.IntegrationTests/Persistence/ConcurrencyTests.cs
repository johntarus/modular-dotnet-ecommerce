using Inventory.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Inventory.IntegrationTests.Persistence;

/// <summary>
/// InventoryItem.Version maps to Postgres xmin. Without it, two concurrent adjustments
/// both read the same stock level and the second silently overwrites the first.
/// </summary>
public class ConcurrencyTests(PostgresFixture db) : InventoryTestBase(db)
{
    [Fact]
    public async Task ConcurrentAdjustments_SecondIsRejected_NoUpdateIsLost()
    {
        var productId = await SeedItemAsync(onHand: 10);

        await using var contextA = NewContext();
        await using var contextB = NewContext();
        var itemA = await contextA.InventoryItems.SingleAsync(i => i.ProductId == productId);
        var itemB = await contextB.InventoryItems.SingleAsync(i => i.ProductId == productId);

        itemA.AdjustStock(5, "delivery A");
        await contextA.SaveChangesAsync();

        itemB.AdjustStock(5, "delivery B");
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => contextB.SaveChangesAsync());

        // Before the fix this ended at 15 with both saves "succeeding" — one delivery lost
        Assert.Equal(15, (await LoadAsync(productId))!.QuantityOnHand);
    }

    [Fact]
    public async Task ConcurrentReservations_CannotOversell()
    {
        var productId = await SeedItemAsync(onHand: 5);

        await using var contextA = NewContext();
        await using var contextB = NewContext();
        var itemA = await contextA.InventoryItems.SingleAsync(i => i.ProductId == productId);
        var itemB = await contextB.InventoryItems.SingleAsync(i => i.ProductId == productId);

        // Each request sees 5 available and reserves 4 — together that would be 8 of 5
        itemA.ReserveStock(4);
        itemB.ReserveStock(4);
        await contextA.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => contextB.SaveChangesAsync());

        var stored = (await LoadAsync(productId))!;
        Assert.Equal(4, stored.QuantityReserved);
        Assert.Equal(1, stored.AvailableQuantity);
    }

    [Fact]
    public async Task SequentialAdjustments_WithFreshLoads_BothSucceed()
    {
        var productId = await SeedItemAsync(onHand: 10);

        foreach (var change in new[] { 5, 5 })
        {
            await using var context = NewContext();
            var item = await context.InventoryItems.SingleAsync(i => i.ProductId == productId);
            item.AdjustStock(change, "delivery");
            await context.SaveChangesAsync();
        }

        Assert.Equal(20, (await LoadAsync(productId))!.QuantityOnHand);
    }
}
