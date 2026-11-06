using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;

namespace Api.IntegrationTests.Catalog;

/// <summary>
/// End-to-end tests for /api/catalog over HTTP: CanManageCatalog on writes, validation,
/// status mapping, and a price change reaching Cart.
/// </summary>
public class CatalogEndpointTests(ApiFactory factory) : ApiTestBase(factory)
{
    private const string Products = "/api/catalog/products";
    private const string Categories = "/api/catalog/categories";

    private async Task<Guid> CreateCategoryAsync()
    {
        var response = await Admin().PostAsJsonAsync(Categories, new { Name = $"Category {Guid.NewGuid():N}", Description = "d" });
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expected, string? title = null)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        if (title is not null)
            Assert.Equal(title, (await ReadJsonAsync(response)).GetProperty("title").GetString());
    }

    // ---------- Authorization ----------

    public static TheoryData<string, string> WriteEndpoints => new()
    {
        { "POST", Products },
        { "PUT", $"{Products}/{Guid.NewGuid()}" },
        { "DELETE", $"{Products}/{Guid.NewGuid()}" },
        { "POST", Categories },
        { "PUT", $"{Categories}/{Guid.NewGuid()}" },
        { "DELETE", $"{Categories}/{Guid.NewGuid()}" },
    };

    private static HttpRequestMessage Request(string method, string url) =>
        new(new HttpMethod(method), url) { Content = method == "DELETE" ? null : JsonContent.Create(new { Name = "x" }) };

    [Theory]
    [MemberData(nameof(WriteEndpoints))]
    public async Task Writes_Anonymous_Return401(string method, string url)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous().SendAsync(Request(method, url))).StatusCode);
    }

    [Theory]
    [MemberData(nameof(WriteEndpoints))]
    public async Task Writes_SignedInWithoutAdminRole_Return403(string method, string url)
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await SignedIn(roles: "Customer").SendAsync(Request(method, url))).StatusCode);
    }

    [Fact]
    public async Task Reads_ArePublic()
    {
        var categoryId = await CreateCategoryAsync();
        var productId = await CreateProductAsync(10m, 1);
        var anonymous = Anonymous();

        foreach (var url in new[] { Products, $"{Products}/{productId}", Categories, $"{Categories}/{categoryId}" })
            Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync(url)).StatusCode);
    }

    // ---------- Categories ----------

    [Fact]
    public async Task CreateCategory_Returns201_WithWorkingLocation()
    {
        var response = await Admin().PostAsJsonAsync(Categories, new { Name = "Garden", Description = "Outdoors" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.Equal($"{Categories}/{id}", response.Headers.Location?.OriginalString);

        var fetched = await Anonymous().GetAsync(response.Headers.Location);
        await EnsureSuccessAsync(fetched);
        Assert.Equal("Garden", (await ReadJsonAsync(fetched)).GetProperty("name").GetString());
    }

    [Fact]
    public async Task UpdateCategory_Unknown_Returns404()
    {
        await AssertProblemAsync(
            await Admin().PutAsJsonAsync($"{Categories}/{Guid.NewGuid()}", new { Name = "x", Description = (string?)null }),
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_Twice_Returns204Then404()
    {
        var id = await CreateCategoryAsync();
        var admin = Admin();

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{Categories}/{id}")).StatusCode);
        await AssertProblemAsync(await admin.DeleteAsync($"{Categories}/{id}"), HttpStatusCode.NotFound);
        await AssertProblemAsync(await Anonymous().GetAsync($"{Categories}/{id}"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_WithProducts_Returns409_AndCategoryRemains()
    {
        var productId = await CreateProductAsync(10m, 1);
        var categoryId = (await ReadJsonAsync(await Anonymous().GetAsync($"{Products}/{productId}"))).GetProperty("categoryId").GetGuid();

        await AssertProblemAsync(await Admin().DeleteAsync($"{Categories}/{categoryId}"), HttpStatusCode.Conflict);
        Assert.Equal(HttpStatusCode.OK, (await Anonymous().GetAsync($"{Categories}/{categoryId}")).StatusCode);
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_IgnoringCase_Returns400()
    {
        var name = $"Category {Guid.NewGuid():N}";
        var admin = Admin();
        await EnsureSuccessAsync(await admin.PostAsJsonAsync(Categories, new { Name = name }));

        await AssertProblemAsync(await admin.PostAsJsonAsync(Categories, new { Name = name.ToUpperInvariant() }),
            HttpStatusCode.BadRequest, "Validation Failed");
    }

    [Fact]
    public async Task CreateProduct_DuplicateNameInCategory_Returns400_ButAllowedInAnotherCategory()
    {
        var first = await CreateCategoryAsync();
        var second = await CreateCategoryAsync();
        var admin = Admin();
        await EnsureSuccessAsync(await admin.PostAsJsonAsync(Products, new { Name = "Mug", Description = "d", Price = 1m, CategoryId = first }));

        await AssertProblemAsync(await admin.PostAsJsonAsync(Products, new { Name = "mug", Description = "d", Price = 1m, CategoryId = first }),
            HttpStatusCode.BadRequest, "Validation Failed");
        Assert.Equal(HttpStatusCode.Created,
            (await admin.PostAsJsonAsync(Products, new { Name = "Mug", Description = "d", Price = 1m, CategoryId = second })).StatusCode);
    }

    [Fact]
    public async Task CreateCategory_NameIsStoredTrimmed_AndPaddedDuplicateIsRejected()
    {
        var name = $"Category {Guid.NewGuid():N}";
        var admin = Admin();

        var created = await admin.PostAsJsonAsync(Categories, new { Name = $"  {name}  " });
        await EnsureSuccessAsync(created);
        var fetched = await ReadJsonAsync(await Anonymous().GetAsync(created.Headers.Location));
        Assert.Equal(name, fetched.GetProperty("name").GetString());

        await AssertProblemAsync(await admin.PostAsJsonAsync(Categories, new { Name = $"{name} " }),
            HttpStatusCode.BadRequest, "Validation Failed");
    }

    [Fact]
    public async Task CreateCategory_BlankName_Returns400()
    {
        await AssertProblemAsync(await Admin().PostAsJsonAsync(Categories, new { Name = "" }), HttpStatusCode.BadRequest, "Validation Failed");
    }

    // ---------- Products ----------

    [Fact]
    public async Task CreateProduct_Returns201_AndIsReadable()
    {
        var categoryId = await CreateCategoryAsync();

        var response = await Admin().PostAsJsonAsync(Products, new { Name = "Mug", Description = "Ceramic", Price = 12.5m, CategoryId = categoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        var product = await ReadJsonAsync(await Anonymous().GetAsync($"{Products}/{id}"));
        Assert.Equal("Mug", product.GetProperty("name").GetString());
        Assert.Equal(12.5m, product.GetProperty("price").GetDecimal());
    }

    [Fact]
    public async Task CreateProduct_MissingDescription_Returns400_Not500()
    {
        var categoryId = await CreateCategoryAsync();

        var response = await Admin().PostAsJsonAsync(Products, new { Name = "Mug", Price = 1m, CategoryId = categoryId });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Validation Failed");
    }

    [Fact]
    public async Task CreateProduct_DescriptionOverColumnLimit_Returns400_Not500()
    {
        var categoryId = await CreateCategoryAsync();

        var response = await Admin().PostAsJsonAsync(Products,
            new { Name = "Mug", Description = new string('d', 1001), Price = 1m, CategoryId = categoryId });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Validation Failed");
    }

    [Fact]
    public async Task CreateProduct_EmptyCategory_ReportsTheErrorOnce()
    {
        var response = await Admin().PostAsJsonAsync(Products, new { Name = "Mug", Description = "d", Price = 1m, CategoryId = Guid.Empty });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Validation Failed");
        var errors = (await ReadJsonAsync(response)).GetProperty("errors").EnumerateArray().Select(e => e.GetString()!).ToList();
        Assert.Single(errors, e => e.Contains("must not be empty"));
    }

    [Fact]
    public async Task CreateProduct_UnknownCategory_Returns400()
    {
        var response = await Admin().PostAsJsonAsync(Products, new { Name = "Mug", Description = "d", Price = 1m, CategoryId = Guid.NewGuid() });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Validation Failed");
    }

    [Fact]
    public async Task UpdateProduct_Returns200WithUpdatedProduct()
    {
        var productId = await CreateProductAsync(10m, 1);
        var categoryId = await CreateCategoryAsync();

        var response = await Admin().PutAsJsonAsync($"{Products}/{productId}",
            new { Name = "Renamed", Description = "New", Price = 20m, CategoryId = categoryId });

        await EnsureSuccessAsync(response);
        var body = await ReadJsonAsync(response);
        Assert.Equal("Renamed", body.GetProperty("name").GetString());
        Assert.Equal(20m, body.GetProperty("price").GetDecimal());
    }

    [Fact]
    public async Task UpdateProduct_Unknown_Returns404_Not500()
    {
        var categoryId = await CreateCategoryAsync();

        var response = await Admin().PutAsJsonAsync($"{Products}/{Guid.NewGuid()}",
            new { Name = "x", Description = "d", Price = 1m, CategoryId = categoryId });

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_Twice_Returns204Then404()
    {
        var productId = await CreateProductAsync(10m, 1);
        var admin = Admin();

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{Products}/{productId}")).StatusCode);
        await AssertProblemAsync(await admin.DeleteAsync($"{Products}/{productId}"), HttpStatusCode.NotFound);
    }

    // ---------- Cross-module ----------

    [Fact]
    public async Task PriceChange_IsUsedByCart_ForNewlyAddedItems()
    {
        var productId = await CreateProductAsync(10m, stock: 10);
        var categoryId = (await ReadJsonAsync(await Anonymous().GetAsync($"{Products}/{productId}"))).GetProperty("categoryId").GetGuid();
        await EnsureSuccessAsync(await Admin().PutAsJsonAsync($"{Products}/{productId}",
            new { Name = "Mug", Description = "d", Price = 14m, CategoryId = categoryId }));

        var user = SignedIn();
        await EnsureSuccessAsync(await user.PostAsJsonAsync("/cart/items", new { ProductId = productId, Quantity = 1 }));

        var item = Assert.Single((await ReadJsonAsync(await user.GetAsync("/cart"))).GetProperty("items").EnumerateArray());
        Assert.Equal(14m, item.GetProperty("unitPrice").GetDecimal());
    }

    [Fact]
    public async Task DeletedProduct_CannotBeAddedToCart()
    {
        var productId = await CreateProductAsync(10m, stock: 10);
        await EnsureSuccessAsync(await Admin().DeleteAsync($"{Products}/{productId}"));

        var response = await SignedIn().PostAsJsonAsync("/cart/items", new { ProductId = productId, Quantity = 1 });

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }
}
