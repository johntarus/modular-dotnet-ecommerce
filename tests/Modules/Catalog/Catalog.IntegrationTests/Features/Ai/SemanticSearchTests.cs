using Catalog.Features.Products.SemanticSearch;
using Catalog.IntegrationTests.Infrastructure;

namespace Catalog.IntegrationTests.Features.Ai;

/// <summary>
/// Vectors live in the first two components, so cosine similarity to the query (1, 0) is just
/// the first component of each unit vector: Kettle 1.0, Toaster 0.8, Blender 0.6, Sofa 0.0.
/// </summary>
public class SemanticSearchTests(PostgresFixture db) : AiTestBase(db)
{
    private readonly FakeEmbeddingService _embeddings = new(Vectors.Of(1f, 0f));

    private async Task SeedCatalogAsync()
    {
        var categoryId = await SeedCategoryAsync();
        await SeedEmbeddedProductAsync(categoryId, "Sofa", Vectors.Of(0f, 1f));
        await SeedEmbeddedProductAsync(categoryId, "Blender", Vectors.Of(0.6f, 0.8f));
        await SeedEmbeddedProductAsync(categoryId, "Kettle", Vectors.Of(1f, 0f));
        await SeedEmbeddedProductAsync(categoryId, "Toaster", Vectors.Of(0.8f, 0.6f));
    }

    private async Task<List<SemanticSearchResult>> SearchAsync(string query, int limit = 20, double minSimilarity = 0)
    {
        await using var context = NewContext();
        return await new SemanticSearchQueryHandler(context, _embeddings)
            .Handle(new SemanticSearchQuery(query, limit, minSimilarity), CancellationToken.None);
    }

    [Fact]
    public async Task RanksByCosineSimilarity()
    {
        await SeedCatalogAsync();

        var results = await SearchAsync("something to boil water");

        Assert.Equal(["Kettle", "Toaster", "Blender", "Sofa"], results.Select(r => r.Name));
        Assert.Equal([1.0, 0.8, 0.6, 0.0], results.Select(r => Math.Round(r.SimilarityScore, 4)));
    }

    /// <summary>mxbai-embed-large needs this instruction on queries (not documents) to retrieve well.</summary>
    [Fact]
    public async Task PrefixesQueryWithRetrievalInstruction()
    {
        await SearchAsync("kettle");

        Assert.Equal("Represent this sentence for searching relevant passages: kettle", Assert.Single(_embeddings.Inputs));
    }

    [Fact]
    public async Task AppliesLimit()
    {
        await SeedCatalogAsync();

        Assert.Equal(["Kettle", "Toaster"], (await SearchAsync("kettle", limit: 2)).Select(r => r.Name));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(500, 4)]
    public async Task ClampsLimitTo1Through100(int limit, int expectedCount)
    {
        await SeedCatalogAsync();

        Assert.Equal(expectedCount, (await SearchAsync("kettle", limit)).Count);
    }

    [Fact]
    public async Task FiltersBelowMinSimilarity()
    {
        await SeedCatalogAsync();

        Assert.Equal(["Kettle", "Toaster"], (await SearchAsync("kettle", minSimilarity: 0.7)).Select(r => r.Name));
    }

    [Fact]
    public async Task SkipsProductsWithoutEmbedding_AndDeletedProducts()
    {
        var categoryId = await SeedCategoryAsync();
        await SeedProductAsync(categoryId, name: "Unembedded");
        await SeedEmbeddedProductAsync(categoryId, "Deleted kettle", Vectors.Of(1f, 0f), deleted: true);
        await SeedEmbeddedProductAsync(categoryId, "Kettle", Vectors.Of(0.8f, 0.6f));

        Assert.Equal(["Kettle"], (await SearchAsync("kettle")).Select(r => r.Name));
    }

    [Fact]
    public async Task ReturnsProductDetails()
    {
        var categoryId = await SeedCategoryAsync();
        var id = await SeedEmbeddedProductAsync(categoryId, "Kettle", Vectors.Of(1f, 0f), price: 2500m, description: "1.7L electric");

        var result = Assert.Single(await SearchAsync("kettle"));

        Assert.Equal(id, result.ProductId);
        Assert.Equal("1.7L electric", result.Description);
        Assert.Equal(2500m, result.Price);
        Assert.Equal(categoryId, result.CategoryId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankQuery_Throws_WithoutEmbedding(string query)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => SearchAsync(query));
        Assert.Empty(_embeddings.Inputs);
    }
}
