using Cart.IntegrationTests.Infrastructure;

namespace Cart.IntegrationTests.Persistence;

public class ExpiredCartCleanupTests(PostgresFixture db) : CartTestBase(db)
{
    private async Task<int> RunCleanupAsync(DateTime now)
    {
        await using var context = NewContext();
        return await ExpiredCartCleanupJob.DeleteExpiredGuestCartsAsync(context, now, CancellationToken.None);
    }

    [Fact]
    public async Task DeletesGuestCartsPastTheirExpiry_IncludingTheirItems()
    {
        var (expired, _) = await SeedGuestCartAsync((Guid.NewGuid(), 2, 10m));

        // Guest carts expire 24h after creation
        var deleted = await RunCleanupAsync(DateTime.UtcNow.AddHours(25));

        Assert.Equal(1, deleted);
        Assert.Null(await LoadCartAsync(expired.Id));
        Assert.Equal(0, await CountCartsAsync());
    }

    [Fact]
    public async Task KeepsGuestCartsThatHaveNotExpired()
    {
        var (fresh, _) = await SeedGuestCartAsync((Guid.NewGuid(), 1, 10m));

        var deleted = await RunCleanupAsync(DateTime.UtcNow.AddHours(23));

        Assert.Equal(0, deleted);
        Assert.NotNull(await LoadCartAsync(fresh.Id));
    }

    [Fact]
    public async Task NeverDeletesUserCarts()
    {
        // User carts have no expiry (ExpiresAt is left at its default), so they
        // would match the date filter on its own; the guest-only filter must protect them.
        var userCart = await SeedUserCartAsync(Guid.NewGuid(), (Guid.NewGuid(), 1, 10m));
        await SeedGuestCartAsync();

        var deleted = await RunCleanupAsync(DateTime.UtcNow.AddYears(1));

        Assert.Equal(1, deleted);
        Assert.NotNull(await LoadCartAsync(userCart.Id));
    }
}
