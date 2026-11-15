using Catalog.Features.Products.Ask;
using Catalog.IntegrationTests.Infrastructure;

namespace Catalog.IntegrationTests.Features.Ai;

/// <summary>
/// The RAG handler keeps products with similarity of at least 0.55 to the question, top 5,
/// and only calls the LLM when something matched.
/// </summary>
public class AskProductsTests(PostgresFixture db) : AiTestBase(db)
{
    private readonly FakeEmbeddingService _embeddings = new(Vectors.Of(1f, 0f));
    private readonly FakeLlmProviderFactory _providers = new(new FakeChatService());
    private FakeChatService _chat => _providers.Chat;

    private async Task<AskProductsResult> AskAsync(string question, string? provider = null)
    {
        await using var context = NewContext();
        return await new AskProductsQueryHandler(context, _embeddings, _providers, new FakeCurrentUser(Guid.NewGuid()))
            .Handle(new AskProductsQuery(question, provider), CancellationToken.None);
    }

    private string Prompt => Assert.Single(_chat.Calls).Single(m => m.Role == "user").Content;

    [Fact]
    public async Task NoRelevantProducts_ReturnsFallback_WithoutCallingLlm()
    {
        var categoryId = await SeedCategoryAsync();
        await SeedEmbeddedProductAsync(categoryId, "Sofa", Vectors.Of(0.5f, 0.866f)); // similarity 0.5 < 0.55

        var result = await AskAsync("a kettle?");

        Assert.StartsWith("I couldn't find any products", result.Answer);
        Assert.Empty(result.Sources);
        Assert.Equal(0, result.InputTokens);
        Assert.Equal(0, result.OutputTokens);
        Assert.Empty(_chat.Calls);
        Assert.Empty(_providers.RequestedProviders);
    }

    [Fact]
    public async Task ReturnsLlmAnswer_TokenUsage_AndSourcesBestFirst()
    {
        var categoryId = await SeedCategoryAsync();
        var toaster = await SeedEmbeddedProductAsync(categoryId, "Toaster", Vectors.Of(0.8f, 0.6f));
        var kettle = await SeedEmbeddedProductAsync(categoryId, "Kettle", Vectors.Of(1f, 0f));

        var result = await AskAsync("something to boil water");

        Assert.Equal("Try the Kettle at KES 2,500.", result.Answer);
        Assert.Equal(120, result.InputTokens);
        Assert.Equal(18, result.OutputTokens);
        Assert.Equal([kettle, toaster], result.Sources.Select(s => s.ProductId));
        Assert.Equal([1.0, 0.8], result.Sources.Select(s => Math.Round(s.SimilarityScore, 4)));
    }

    [Fact]
    public async Task OnlyProductsAtOrAboveThreshold_AreSourcesAndInPrompt()
    {
        var categoryId = await SeedCategoryAsync();
        await SeedEmbeddedProductAsync(categoryId, "Kettle", Vectors.Of(1f, 0f));
        await SeedEmbeddedProductAsync(categoryId, "Blender", Vectors.Of(0.6f, 0.8f));   // 0.60 — kept
        await SeedEmbeddedProductAsync(categoryId, "Sofa", Vectors.Of(0.5f, 0.866f));    // 0.50 — dropped

        var result = await AskAsync("kettle");

        Assert.Equal(["Kettle", "Blender"], result.Sources.Select(s => s.Name));
        Assert.DoesNotContain("Sofa", Prompt);
    }

    [Fact]
    public async Task CapsSourcesAtFive()
    {
        var categoryId = await SeedCategoryAsync();
        for (var i = 0; i < 7; i++)
            await SeedEmbeddedProductAsync(categoryId, $"Kettle {i}", Vectors.Of(1f, i * 0.01f));

        var result = await AskAsync("kettle");

        Assert.Equal(5, result.Sources.Count);
        Assert.Equal(["Kettle 0", "Kettle 1", "Kettle 2", "Kettle 3", "Kettle 4"], result.Sources.Select(s => s.Name));
    }

    [Fact]
    public async Task ExcludesDeletedProducts()
    {
        var categoryId = await SeedCategoryAsync();
        await SeedEmbeddedProductAsync(categoryId, "Deleted kettle", Vectors.Of(1f, 0f), deleted: true);

        var result = await AskAsync("kettle");

        Assert.Empty(result.Sources);
        Assert.Empty(_chat.Calls);
    }

    [Fact]
    public async Task PromptContainsNumberedProducts_PricesAndQuestion()
    {
        var categoryId = await SeedCategoryAsync();
        await SeedEmbeddedProductAsync(categoryId, "Kettle", Vectors.Of(1f, 0f), price: 2500m, description: "1.7L electric");
        await SeedEmbeddedProductAsync(categoryId, "Toaster", Vectors.Of(0.8f, 0.6f), price: 3200m, description: "2-slice");

        await AskAsync("something to boil water");

        var messages = Assert.Single(_chat.Calls);
        Assert.Equal(["system", "user"], messages.Select(m => m.Role));
        Assert.Contains("[1] Kettle — 1.7L electric — KES 2,500", Prompt);
        Assert.Contains("[2] Toaster — 2-slice — KES 3,200", Prompt);
        Assert.Contains("QUESTION: something to boil water", Prompt);
    }

    [Fact]
    public async Task EmbedsQuestionWithRetrievalInstruction()
    {
        await AskAsync("kettle");

        Assert.Equal("Represent this sentence for searching relevant passages: kettle", Assert.Single(_embeddings.Inputs));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("OpenAI")]
    public async Task PassesRequestedProviderToFactory(string? provider)
    {
        var categoryId = await SeedCategoryAsync();
        await SeedEmbeddedProductAsync(categoryId, "Kettle", Vectors.Of(1f, 0f));

        await AskAsync("kettle", provider);

        Assert.Equal([provider], _providers.RequestedProviders);
    }
}
