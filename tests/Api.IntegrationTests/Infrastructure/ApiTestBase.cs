using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.IntegrationTests.Infrastructure;

[Collection(ApiCollection.Name)]
public abstract class ApiTestBase(ApiFactory factory) : IAsyncLifetime
{
    protected ApiFactory Factory { get; } = factory;

    public Task InitializeAsync() => Factory.ResetCartsAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>A client with no identity and no cart token.</summary>
    protected HttpClient Anonymous() => Factory.CreateClient();

    protected HttpClient SignedIn(Guid? userId = null)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, (userId ?? Guid.NewGuid()).ToString());
        return client;
    }

    protected static HttpClient WithCartToken(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Remove("X-Cart-Token");
        client.DefaultRequestHeaders.Add("X-Cart-Token", token);
        return client;
    }

    /// <summary>
    /// Creates a real product through the Catalog API, waits for Inventory to receive the
    /// ProductCreated event over RabbitMQ, then sets its stock through the Inventory API.
    /// </summary>
    protected async Task<Guid> CreateProductAsync(decimal price, int stock)
    {
        var admin = Anonymous();

        var categoryResponse = await admin.PostAsJsonAsync("/api/catalog/categories",
            new { Name = $"Category {Guid.NewGuid():N}", Description = "Test category" });
        await EnsureSuccessAsync(categoryResponse);
        var categoryId = await categoryResponse.Content.ReadFromJsonAsync<Guid>();

        var productResponse = await admin.PostAsJsonAsync("/api/catalog/products",
            new { Name = $"Product {Guid.NewGuid():N}", Description = "Test product", Price = price, CategoryId = categoryId });
        await EnsureSuccessAsync(productResponse);
        var productId = await productResponse.Content.ReadFromJsonAsync<Guid>();

        await WaitForInventoryRecordAsync(admin, productId);

        var stockResponse = await admin.PutAsJsonAsync($"/inventory/{productId}/stock",
            new { Quantity = stock, Reason = "test setup" });
        await EnsureSuccessAsync(stockResponse);

        return productId;
    }

    private static async Task WaitForInventoryRecordAsync(HttpClient client, Guid productId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            var response = await client.GetAsync($"/inventory/{productId}");
            if (response.StatusCode == HttpStatusCode.OK) return;
            await Task.Delay(200);
        }
        throw new TimeoutException($"Inventory never received ProductCreated for {productId}.");
    }

    protected static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"{(int)response.StatusCode} {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}: {await response.Content.ReadAsStringAsync()}");
    }

    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
}
