using Cart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Cart.IntegrationTests.Persistence;

/// <summary>
/// ShoppingCart.Version maps to Postgres xmin, so two requests that load the same cart
/// and both save must not silently overwrite each other.
/// </summary>
public class ConcurrencyTests(PostgresFixture db) : CartTestBase(db)
{
    [Fact]
    public async Task SecondConcurrentWrite_IsRejected_AndFirstWriteIsKept()
    {
        var userId = Guid.NewGuid();
        var seeded = await SeedUserCartAsync(userId, (Guid.NewGuid(), 1, 10m));
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        await using var contextA = NewContext();
        await using var contextB = NewContext();
        var cartA = await contextA.Carts.Include(c => c.Items).SingleAsync(c => c.Id == seeded.Id);
        var cartB = await contextB.Carts.Include(c => c.Items).SingleAsync(c => c.Id == seeded.Id);

        cartA.AddItem(first, 1, 10m);
        await contextA.SaveChangesAsync();

        cartB.AddItem(second, 1, 10m);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => contextB.SaveChangesAsync());

        var stored = (await LoadCartAsync(seeded.Id))!;
        Assert.Contains(stored.Items, i => i.ProductId == first);
        Assert.DoesNotContain(stored.Items, i => i.ProductId == second);
    }

    [Fact]
    public async Task SequentialWrites_WithFreshLoads_BothSucceed()
    {
        var seeded = await SeedUserCartAsync(Guid.NewGuid());

        for (var i = 0; i < 2; i++)
        {
            await using var context = NewContext();
            var cart = await context.Carts.Include(c => c.Items).SingleAsync(c => c.Id == seeded.Id);
            cart.AddItem(Guid.NewGuid(), 1, 10m);
            await context.SaveChangesAsync();
        }

        Assert.Equal(2, (await LoadCartAsync(seeded.Id))!.Items.Count);
    }
}
