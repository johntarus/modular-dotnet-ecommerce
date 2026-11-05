using BuildingBlocks.Exceptions;
using Cart.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Resolver = Cart.Features.Carts.CartResolver.CartResolver;

namespace Cart.IntegrationTests.Features;

public class CartResolverTests(PostgresFixture db) : CartTestBase(db)
{
    // ---------- Signed-in user ----------

    [Fact]
    public async Task User_WithExistingCart_ReturnsItWithoutIssuingToken()
    {
        var userId = SignIn();
        var seeded = await SeedUserCartAsync(userId, (Guid.NewGuid(), 2, 10m));

        await using var context = NewContext();
        var resolved = await NewResolver(context).ResolveAsync();

        Assert.Equal(seeded.Id, resolved.Cart.Id);
        Assert.Single(resolved.Cart.Items);
        Assert.Null(resolved.NewlyIssuedToken);
    }

    [Fact]
    public async Task User_WithoutCart_CreatesAndPersistsOne()
    {
        var userId = SignIn();

        await using var context = NewContext();
        var resolved = await NewResolver(context).ResolveAsync(createIfMissing: true);

        Assert.Null(resolved.NewlyIssuedToken);
        var stored = await LoadCartAsync(resolved.Cart.Id);
        Assert.NotNull(stored);
        Assert.Equal(userId, stored.UserId);
    }

    [Fact]
    public async Task User_WithoutCart_AndNoCreate_ThrowsNotFound()
    {
        SignIn();

        await using var context = NewContext();
        await Assert.ThrowsAsync<NotFoundException>(() => NewResolver(context).ResolveAsync(createIfMissing: false));
        Assert.Equal(0, await CountCartsAsync());
    }

    [Fact]
    public async Task User_IgnoresGuestTokenHeader()
    {
        var userId = SignIn();
        var userCart = await SeedUserCartAsync(userId);
        var (_, guestToken) = await SeedGuestCartAsync((Guid.NewGuid(), 1, 5m));
        SendGuestToken(guestToken);

        await using var context = NewContext();
        var resolved = await NewResolver(context).ResolveAsync();

        Assert.Equal(userCart.Id, resolved.Cart.Id);
    }

    // ---------- Guest ----------

    [Fact]
    public async Task Guest_WithoutToken_CreatesCartAndIssuesToken()
    {
        await using var context = NewContext();
        var resolved = await NewResolver(context).ResolveAsync(createIfMissing: true);

        Assert.False(string.IsNullOrWhiteSpace(resolved.NewlyIssuedToken));
        var stored = await LoadCartAsync(resolved.Cart.Id);
        Assert.NotNull(stored);
        Assert.Null(stored.UserId);
        // Only the hash is stored, never the raw token
        Assert.Equal(Resolver.HashToken(resolved.NewlyIssuedToken!), stored.GuestTokenHash);
        Assert.NotEqual(resolved.NewlyIssuedToken, stored.GuestTokenHash);
    }

    [Fact]
    public async Task Guest_WithoutToken_IssuesDifferentTokenEachTime()
    {
        await using var context = NewContext();
        var first = await NewResolver(context).ResolveAsync();
        var second = await NewResolver(context).ResolveAsync();

        Assert.NotEqual(first.NewlyIssuedToken, second.NewlyIssuedToken);
        Assert.NotEqual(first.Cart.Id, second.Cart.Id);
    }

    [Fact]
    public async Task Guest_WithoutToken_AndNoCreate_ThrowsBadRequest()
    {
        await using var context = NewContext();

        await Assert.ThrowsAsync<BadHttpRequestException>(() => NewResolver(context).ResolveAsync(createIfMissing: false));
        Assert.Equal(0, await CountCartsAsync());
    }

    [Fact]
    public async Task Guest_WithValidToken_ReturnsExistingCartWithoutNewToken()
    {
        var (seeded, token) = await SeedGuestCartAsync((Guid.NewGuid(), 3, 7m));
        SendGuestToken(token);

        await using var context = NewContext();
        var resolved = await NewResolver(context).ResolveAsync();

        Assert.Equal(seeded.Id, resolved.Cart.Id);
        Assert.Single(resolved.Cart.Items);
        Assert.Null(resolved.NewlyIssuedToken);
        Assert.Equal(1, await CountCartsAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Guest_WithUnknownToken_ThrowsNotFoundAndNeverCreates(bool createIfMissing)
    {
        SendGuestToken("token-that-matches-nothing");

        await using var context = NewContext();

        await Assert.ThrowsAsync<NotFoundException>(() => NewResolver(context).ResolveAsync(createIfMissing));
        Assert.Equal(0, await CountCartsAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetGuestToken_TreatsBlankHeaderAsMissing(string header)
    {
        SendGuestToken(header);

        using var context = NewContext();
        Assert.Null(NewResolver(context).GetGuestToken());
    }
}
