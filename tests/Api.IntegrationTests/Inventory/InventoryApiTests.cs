using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;

namespace Api.IntegrationTests.Inventory;

/// <summary>
/// End-to-end tests for /inventory over HTTP: authorization on stock changes, validation,
/// exception-to-status mapping, and the Catalog → RabbitMQ → Inventory event flow.
/// </summary>
public class InventoryApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    private static Task<HttpResponseMessage> SetStockAsync(HttpClient client, Guid productId, int quantity, string? reason = "recount") =>
        client.PutAsJsonAsync($"/inventory/{productId}/stock", new { Quantity = quantity, Reason = reason });

    private static Task<HttpResponseMessage> AdjustAsync(HttpClient client, Guid productId, int change, string? reason = "delivery") =>
        client.PatchAsJsonAsync($"/inventory/{productId}/adjustments", new { QuantityChange = change, Reason = reason });

    private async Task<int> OnHandAsync(Guid productId)
    {
        var response = await Anonymous().GetAsync($"/inventory/{productId}");
        await EnsureSuccessAsync(response);
        return (await ReadJsonAsync(response)).GetProperty("quantityOnHand").GetInt32();
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expected, string? title = null)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        if (title is not null)
            Assert.Equal(title, (await ReadJsonAsync(response)).GetProperty("title").GetString());
    }

    // ---------- Reads (public) ----------

    [Fact]
    public async Task GetById_ReturnsStock_WithoutSignIn()
    {
        var productId = await CreateProductAsync(10m, stock: 7);

        var response = await Anonymous().GetAsync($"/inventory/{productId}");

        await EnsureSuccessAsync(response);
        var body = await ReadJsonAsync(response);
        Assert.Equal(productId, body.GetProperty("productId").GetGuid());
        Assert.Equal(7, body.GetProperty("quantityOnHand").GetInt32());
        Assert.Equal(7, body.GetProperty("availableQuantity").GetInt32());
    }

    [Fact]
    public async Task GetById_UnknownProduct_Returns404()
    {
        await AssertProblemAsync(await Anonymous().GetAsync($"/inventory/{Guid.NewGuid()}"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetList_IsPaged_AndClampsPageSize()
    {
        await CreateProductAsync(10m, 1);

        var response = await Anonymous().GetAsync("/inventory?page=1&pageSize=1000");

        await EnsureSuccessAsync(response);
        var body = await ReadJsonAsync(response);
        Assert.Equal(100, body.GetProperty("pageSize").GetInt32());
        Assert.True(body.GetProperty("totalCount").GetInt64() >= 1);
        Assert.NotEqual(0, body.GetProperty("items").GetArrayLength());
    }

    // ---------- Authorization on stock changes ----------

    [Fact]
    public async Task SetStock_Anonymous_Returns401()
    {
        var productId = await CreateProductAsync(10m, 5);

        var response = await SetStockAsync(Anonymous(), productId, 50);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(5, await OnHandAsync(productId));
    }

    [Fact]
    public async Task SetStock_SignedInWithoutAdminRole_Returns403()
    {
        var productId = await CreateProductAsync(10m, 5);

        var response = await SetStockAsync(SignedIn(), productId, 50);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(5, await OnHandAsync(productId));
    }

    [Fact]
    public async Task Adjust_Anonymous_Returns401()
    {
        var productId = await CreateProductAsync(10m, 5);

        Assert.Equal(HttpStatusCode.Unauthorized, (await AdjustAsync(Anonymous(), productId, 1)).StatusCode);
    }

    [Fact]
    public async Task Adjust_SignedInWithoutAdminRole_Returns403()
    {
        var productId = await CreateProductAsync(10m, 5);

        Assert.Equal(HttpStatusCode.Forbidden, (await AdjustAsync(SignedIn(roles: "Customer"), productId, 1)).StatusCode);
        Assert.Equal(5, await OnHandAsync(productId));
    }

    // ---------- Set stock (admin) ----------

    [Fact]
    public async Task SetStock_Admin_Returns204_AndReplacesStock()
    {
        var productId = await CreateProductAsync(10m, 5);

        var response = await SetStockAsync(Admin(), productId, 42);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(42, await OnHandAsync(productId));
    }

    [Fact]
    public async Task SetStock_Negative_Returns400()
    {
        var productId = await CreateProductAsync(10m, 5);

        await AssertProblemAsync(await SetStockAsync(Admin(), productId, -1), HttpStatusCode.BadRequest, "Validation Failed");
    }

    [Fact]
    public async Task SetStock_MissingReason_Returns400()
    {
        var productId = await CreateProductAsync(10m, 5);

        await AssertProblemAsync(await SetStockAsync(Admin(), productId, 5, reason: ""), HttpStatusCode.BadRequest, "Validation Failed");
    }

    [Fact]
    public async Task SetStock_UnknownProduct_Returns404()
    {
        await AssertProblemAsync(await SetStockAsync(Admin(), Guid.NewGuid(), 5), HttpStatusCode.NotFound);
    }

    // ---------- Adjust stock (admin) ----------

    [Fact]
    public async Task Adjust_Admin_AddsAndRemoves()
    {
        var productId = await CreateProductAsync(10m, 5);
        var admin = Admin();

        Assert.Equal(HttpStatusCode.NoContent, (await AdjustAsync(admin, productId, 10)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await AdjustAsync(admin, productId, -3)).StatusCode);

        Assert.Equal(12, await OnHandAsync(productId));
    }

    [Fact]
    public async Task Adjust_BelowZero_Returns409_AndStockUnchanged()
    {
        var productId = await CreateProductAsync(10m, 5);

        await AssertProblemAsync(await AdjustAsync(Admin(), productId, -6), HttpStatusCode.Conflict);
        Assert.Equal(5, await OnHandAsync(productId));
    }

    [Fact]
    public async Task Adjust_Zero_Returns400()
    {
        var productId = await CreateProductAsync(10m, 5);

        await AssertProblemAsync(await AdjustAsync(Admin(), productId, 0), HttpStatusCode.BadRequest, "Validation Failed");
    }

    [Fact]
    public async Task Adjust_MissingReason_Returns400FromValidator()
    {
        var productId = await CreateProductAsync(10m, 5);

        var response = await AdjustAsync(Admin(), productId, 1, reason: "");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Validation Failed");
        var errors = (await ReadJsonAsync(response)).GetProperty("errors").EnumerateArray().Select(e => e.GetString());
        Assert.Contains("A reason is required.", errors);
    }

    [Fact]
    public async Task Adjust_UnknownProduct_Returns404()
    {
        await AssertProblemAsync(await AdjustAsync(Admin(), Guid.NewGuid(), 1), HttpStatusCode.NotFound);
    }

    // ---------- Stock changes reach Cart's gRPC check ----------

    [Fact]
    public async Task StockChange_IsSeenByCartStockCheck()
    {
        var productId = await CreateProductAsync(10m, stock: 1);
        var user = SignedIn();

        var before = await user.PostAsJsonAsync("/cart/items", new { ProductId = productId, Quantity = 3 });
        await EnsureSuccessAsync(await AdjustAsync(Admin(), productId, 5));
        var after = await user.PostAsJsonAsync("/cart/items", new { ProductId = productId, Quantity = 3 });

        Assert.Equal(HttpStatusCode.BadRequest, before.StatusCode);
        await EnsureSuccessAsync(after);
    }

    // ---------- Event flow ----------

    [Fact]
    public async Task DeletingProductInCatalog_RemovesItFromInventory()
    {
        var productId = await CreateProductAsync(10m, 5);

        await EnsureSuccessAsync(await Admin().DeleteAsync($"/api/catalog/products/{productId}"));

        var deadline = DateTime.UtcNow.AddSeconds(30);
        HttpStatusCode status;
        do
        {
            status = (await Anonymous().GetAsync($"/inventory/{productId}")).StatusCode;
            if (status == HttpStatusCode.NotFound) break;
            await Task.Delay(200);
        } while (DateTime.UtcNow < deadline);

        Assert.Equal(HttpStatusCode.NotFound, status);
    }
}
