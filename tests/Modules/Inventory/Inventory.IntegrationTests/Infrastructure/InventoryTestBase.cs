using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.IntegrationTests.Infrastructure;

[Collection(PostgresCollection.Name)]
public abstract class InventoryTestBase(PostgresFixture db) : IAsyncLifetime
{
    protected PostgresFixture Db { get; } = db;

    public Task InitializeAsync() => Db.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    protected InventoryDbContext NewContext() => Db.CreateContext();

    /// <summary>Seeds an inventory record with the given stock, reservation and deleted state.</summary>
    protected async Task<Guid> SeedItemAsync(int onHand = 0, int reserved = 0, bool deleted = false)
    {
        var item = InventoryItem.Create(Guid.NewGuid());
        if (onHand > 0) item.SetStock(onHand, "seed");
        if (reserved > 0) item.ReserveStock(reserved);
        if (deleted) item.Delete();

        await using var context = NewContext();
        context.InventoryItems.Add(item);
        await context.SaveChangesAsync();
        return item.ProductId;
    }

    /// <summary>Reads straight from the database, including soft-deleted records.</summary>
    protected async Task<InventoryItem?> LoadAsync(Guid productId)
    {
        await using var context = NewContext();
        return await context.InventoryItems.AsNoTracking().IgnoreQueryFilters()
            .SingleOrDefaultAsync(i => i.ProductId == productId);
    }

    protected async Task<int> CountAsync()
    {
        await using var context = NewContext();
        return await context.InventoryItems.IgnoreQueryFilters().CountAsync();
    }
}
