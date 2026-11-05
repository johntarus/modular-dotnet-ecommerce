using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.IntegrationTests.Infrastructure;

namespace Api.IntegrationTests.Cart;

/// <summary>
/// End-to-end tests for /cart over HTTP: real Catalog, real Inventory (via gRPC),
/// real validation pipeline, exception-to-status mapping and authorization.
/// </summary>
public class CartApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    private static Task<HttpResponseMessage> AddAsync(HttpClient client, Guid productId, int quantity) =>
        client.PostAsJsonAsync("/cart/items", new { ProductId = productId, Quantity = quantity });

    private static Task<HttpResponseMessage> SetQuantityAsync(HttpClient client, Guid productId, int quantity) =>
        client.PutAsJsonAsync($"/cart/items/{productId}", new { Quantity = quantity });

    private static async Task<JsonElement> GetCartAsync(HttpClient client)
    {
        var response = await client.GetAsync("/cart");
        await EnsureSuccessAsync(response);
        return await ReadJsonAsync(response);
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadJsonAsync(response);
        Assert.Equal((int)expected, problem.GetProperty("status").GetInt32());
    }

    // ---------- Guest flow ----------

    [Fact]
    public async Task Guest_AddItem_GetsTokenAndCatalogPrice()
    {
        var productId = await CreateProductAsync(price: 24.99m, stock: 10);
        var guest = Anonymous();

        var response = await AddAsync(guest, productId, 2);

        await EnsureSuccessAsync(response);
        var body = await ReadJsonAsync(response);
        var token = body.GetProperty("issuedGuestToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        var cart = await GetCartAsync(WithCartToken(guest, token!));
        var item = Assert.Single(cart.GetProperty("items").EnumerateArray());
        Assert.Equal(productId, item.GetProperty("productId").GetGuid());
        Assert.Equal(24.99m, item.GetProperty("unitPrice").GetDecimal());
        Assert.Equal(49.98m, cart.GetProperty("subtotal").GetDecimal());
    }

    [Fact]
    public async Task Guest_SendingTokenBack_KeepsSameCart_AndGetsNoNewToken()
    {
        var a = await CreateProductAsync(10m, 10);
        var b = await CreateProductAsync(20m, 10);
        var guest = Anonymous();

        var first = await ReadJsonAsync(await AddAsync(guest, a, 1));
        WithCartToken(guest, first.GetProperty("issuedGuestToken").GetString()!);
        var secondResponse = await AddAsync(guest, b, 1);
        await EnsureSuccessAsync(secondResponse);
        var second = await ReadJsonAsync(secondResponse);

        Assert.Equal(first.GetProperty("cartId").GetGuid(), second.GetProperty("cartId").GetGuid());
        Assert.Equal(JsonValueKind.Null, second.GetProperty("issuedGuestToken").ValueKind);
        Assert.Equal(2, (await GetCartAsync(guest)).GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Guest_WithoutToken_GetsEmptyCart()
    {
        var cart = await GetCartAsync(Anonymous());

        Assert.Equal(Guid.Empty, cart.GetProperty("id").GetGuid());
        Assert.Equal(0, cart.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Guest_WithUnknownToken_Returns404()
    {
        var response = await WithCartToken(Anonymous(), "not-a-real-token").GetAsync("/cart");

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Guest_CannotChangeQuantity_Returns403()
    {
        var productId = await CreateProductAsync(10m, 10);

        var response = await SetQuantityAsync(Anonymous(), productId, 2);

        await AssertProblemAsync(response, HttpStatusCode.Forbidden);
    }

    // ---------- Add item: errors ----------

    [Fact]
    public async Task AddItem_UnknownProduct_Returns404()
    {
        var response = await AddAsync(SignedIn(), Guid.NewGuid(), 1);

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddItem_MoreThanInStock_Returns400_FromRealInventory()
    {
        var productId = await CreateProductAsync(10m, stock: 3);

        var response = await AddAsync(SignedIn(), productId, 4);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddItem_ExistingPlusNewOverStock_Returns400()
    {
        var productId = await CreateProductAsync(10m, stock: 5);
        var user = SignedIn();
        await EnsureSuccessAsync(await AddAsync(user, productId, 3));

        var response = await AddAsync(user, productId, 3);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        var item = Assert.Single((await GetCartAsync(user)).GetProperty("items").EnumerateArray());
        Assert.Equal(3, item.GetProperty("quantity").GetInt32());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AddItem_InvalidQuantity_Returns400WithValidationErrors(int quantity)
    {
        var response = await AddAsync(SignedIn(), Guid.NewGuid(), quantity);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        var problem = await ReadJsonAsync(response);
        Assert.Equal("Validation Failed", problem.GetProperty("title").GetString());
        Assert.NotEqual(0, problem.GetProperty("errors").GetArrayLength());
    }

    // ---------- Signed-in user ----------

    [Fact]
    public async Task User_SetQuantity_UpdatesCart()
    {
        var productId = await CreateProductAsync(10m, 10);
        var user = SignedIn();
        await EnsureSuccessAsync(await AddAsync(user, productId, 1));

        var response = await SetQuantityAsync(user, productId, 4);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var item = Assert.Single((await GetCartAsync(user)).GetProperty("items").EnumerateArray());
        Assert.Equal(4, item.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task User_SetQuantity_OverStock_Returns400()
    {
        var productId = await CreateProductAsync(10m, stock: 2);
        var user = SignedIn();
        await EnsureSuccessAsync(await AddAsync(user, productId, 1));

        var response = await SetQuantityAsync(user, productId, 3);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public async Task User_SetQuantity_OutOfRange_Returns400(int quantity)
    {
        var productId = await CreateProductAsync(10m, 10);
        var user = SignedIn();
        await EnsureSuccessAsync(await AddAsync(user, productId, 1));

        var response = await SetQuantityAsync(user, productId, quantity);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task User_RemoveItem_Returns204_ThenMissingItemReturns404()
    {
        var productId = await CreateProductAsync(10m, 10);
        var user = SignedIn();
        await EnsureSuccessAsync(await AddAsync(user, productId, 1));

        var removed = await user.DeleteAsync($"/cart/items/{productId}");
        var removedAgain = await user.DeleteAsync($"/cart/items/{productId}");

        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        await AssertProblemAsync(removedAgain, HttpStatusCode.NotFound);
        Assert.Equal(0, (await GetCartAsync(user)).GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task User_ClearCart_EmptiesIt()
    {
        var user = SignedIn();
        await EnsureSuccessAsync(await AddAsync(user, await CreateProductAsync(10m, 10), 1));
        await EnsureSuccessAsync(await AddAsync(user, await CreateProductAsync(20m, 10), 2));

        var response = await user.DeleteAsync("/cart/cart");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, (await GetCartAsync(user)).GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task User_WithoutCart_GetCart_Returns404()
    {
        var response = await SignedIn().GetAsync("/cart");

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    // ---------- Merge ----------

    [Fact]
    public async Task Merge_WithoutSignIn_Returns401()
    {
        var response = await Anonymous().PostAsync("/cart/merge", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Merge_MovesGuestItemsIntoUserCart_AndGuestTokenStopsWorking()
    {
        var productId = await CreateProductAsync(15m, 10);
        var guest = Anonymous();
        var added = await ReadJsonAsync(await AddAsync(guest, productId, 2));
        var token = added.GetProperty("issuedGuestToken").GetString()!;

        var user = WithCartToken(SignedIn(), token);
        var response = await user.PostAsync("/cart/merge", null);

        await EnsureSuccessAsync(response);
        var result = await ReadJsonAsync(response);
        Assert.Equal(1, result.GetProperty("mergedItemCount").GetInt32());

        user.DefaultRequestHeaders.Remove("X-Cart-Token");
        var userCart = await GetCartAsync(user);
        Assert.Equal(result.GetProperty("cartId").GetGuid(), userCart.GetProperty("id").GetGuid());
        var item = Assert.Single(userCart.GetProperty("items").EnumerateArray());
        Assert.Equal(2, item.GetProperty("quantity").GetInt32());

        var guestCart = await WithCartToken(Anonymous(), token).GetAsync("/cart");
        Assert.Equal(HttpStatusCode.NotFound, guestCart.StatusCode);
    }

    [Fact]
    public async Task Merge_WithoutCartToken_ReturnsWarning()
    {
        var response = await SignedIn().PostAsync("/cart/merge", null);

        await EnsureSuccessAsync(response);
        var result = await ReadJsonAsync(response);
        Assert.Equal(0, result.GetProperty("mergedItemCount").GetInt32());
        Assert.NotEqual(0, result.GetProperty("warnings").GetArrayLength());
    }
}
