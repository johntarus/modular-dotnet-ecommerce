using Cart.Domain.Entities;
using Cart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cart.IntegrationTests.Persistence;

/// <summary>
/// The schema is the last line of defence if application code ever bypasses the domain rules.
/// These tests check the constraints the migrations create.
/// </summary>
public class DatabaseConstraintTests(PostgresFixture db) : CartTestBase(db)
{
    private const string UniqueViolation = "23505";
    private const string CheckViolation = "23514";

    private async Task<PostgresException> SaveExpectingFailureAsync(ShoppingCart cart)
    {
        await using var context = NewContext();
        context.Carts.Add(cart);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        return Assert.IsType<PostgresException>(ex.InnerException);
    }

    [Fact]
    public async Task UserCanOnlyHaveOneCart()
    {
        var userId = Guid.NewGuid();
        await SeedUserCartAsync(userId);

        var pg = await SaveExpectingFailureAsync(ShoppingCart.CreateForUser(userId));

        Assert.Equal(UniqueViolation, pg.SqlState);
    }

    [Fact]
    public async Task GuestTokenHashMustBeUnique()
    {
        var (existing, _) = await SeedGuestCartAsync();

        var pg = await SaveExpectingFailureAsync(ShoppingCart.CreateForGuest(existing.GuestTokenHash!));

        Assert.Equal(UniqueViolation, pg.SqlState);
    }

    [Fact]
    public async Task CartMustBelongToAUserOrAGuest()
    {
        // Neither UserId nor GuestTokenHash set
        var pg = await SaveExpectingFailureAsync(ShoppingCart.CreateForGuest(null!));

        Assert.Equal(CheckViolation, pg.SqlState);
        Assert.Equal("CK_Cart_Identity", pg.ConstraintName);
    }

    [Fact]
    public async Task DifferentUsers_CanEachHaveACart()
    {
        await SeedUserCartAsync(Guid.NewGuid());
        await SeedUserCartAsync(Guid.NewGuid());

        Assert.Equal(2, await CountCartsAsync());
    }

    [Fact]
    public async Task UnitPrice_IsStoredWithTwoDecimalPlaces()
    {
        var cart = await SeedUserCartAsync(Guid.NewGuid(), (Guid.NewGuid(), 1, 19.99m));

        Assert.Equal(19.99m, Assert.Single((await LoadCartAsync(cart.Id))!.Items).UnitPrice);
    }
}
