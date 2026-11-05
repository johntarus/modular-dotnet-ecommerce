using Resolver = Cart.Features.Carts.CartResolver.CartResolver;

namespace Cart.UnitTests.Features;

public class GuestTokenHashTests
{
    [Fact]
    public void HashToken_IsDeterministic()
    {
        Assert.Equal(Resolver.HashToken("abc"), Resolver.HashToken("abc"));
    }

    [Fact]
    public void HashToken_DifferentTokens_GiveDifferentHashes()
    {
        Assert.NotEqual(Resolver.HashToken("abc"), Resolver.HashToken("abd"));
    }

    [Fact]
    public void HashToken_IsSha256HexAndNeverTheRawToken()
    {
        var hash = Resolver.HashToken("my-guest-token");

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9A-F]{64}$", hash);
        Assert.NotEqual("my-guest-token", hash);
    }

    [Fact]
    public void HashToken_MatchesKnownSha256Value()
    {
        // SHA-256("abc") — guards against the algorithm or encoding changing,
        // which would orphan every existing guest cart.
        Assert.Equal(
            "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD",
            Resolver.HashToken("abc"));
    }
}
