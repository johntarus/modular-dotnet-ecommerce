using Ai.Contracts;
using Ai.Infrastructure;
using Ai.IntegrationTests.Infrastructure;
using Catalog.Domain.Entities;
using Catalog.Features.Products.Ask;
using Catalog.Features.Products.EventHandlers;
using Catalog.Features.Products.SemanticSearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ai.IntegrationTests.Catalog;

/// <summary>
/// The whole AI path with nothing faked: real Ollama embeddings stored in the real pgvector column
/// by GenerateEmbeddingConsumer, then retrieved by semantic search and the RAG handler.
/// Assertions stick to what a working pipeline guarantees (ranking, the top source), not model wording.
/// The exact ranking/threshold maths is covered with controlled vectors in Catalog.IntegrationTests.
/// </summary>
[Collection(OllamaCollection.Name)]
public sealed class CatalogRagEndToEndTests : IClassFixture<CatalogDatabaseFixture>, IAsyncLifetime
{
    private readonly CatalogDatabaseFixture _db;
    private readonly ServiceProvider _ai;
    private IEmbeddingService Embeddings => _ai.GetRequiredService<IEmbeddingService>();

    public CatalogRagEndToEndTests(CatalogDatabaseFixture db, OllamaFixture ollama)
    {
        _db = db;
        _ai = new ServiceCollection()
            .AddAi(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:OllamaBaseUrl"] = ollama.BaseUrl,
                ["Ai:OllamaEmbeddingModel"] = OllamaFixture.EmbeddingModel,
                ["Ai:OllamaChatModel"] = OllamaFixture.ChatModel,
            }).Build())
            .BuildServiceProvider();
    }

    public Task InitializeAsync() => _db.ResetAsync();

    public async Task DisposeAsync() => await _ai.DisposeAsync();

    private async Task<Guid> SeedAndEmbedAsync(Guid categoryId, string name, string description, decimal price)
    {
        var product = Product.Create(name, description, price, categoryId);
        await using (var context = _db.CreateContext())
        {
            context.Products.Add(product);
            await context.SaveChangesAsync();
        }

        await using (var context = _db.CreateContext())
        {
            await new GenerateEmbeddingConsumer(context, Embeddings, NullLogger<GenerateEmbeddingConsumer>.Instance)
                .GenerateEmbeddingAsync(product.Id, CancellationToken.None);
        }

        return product.Id;
    }

    private async Task<Guid> SeedCatalogAsync()
    {
        var category = Category.Create("Home", null);
        await using (var context = _db.CreateContext())
        {
            context.Categories.Add(category);
            await context.SaveChangesAsync();
        }

        var kettle = await SeedAndEmbedAsync(category.Id, "Electric Kettle", "1.7L stainless steel kettle that boils water in minutes.", 2500m);
        await SeedAndEmbedAsync(category.Id, "Leather Sofa", "Three-seater sofa upholstered in dark brown leather.", 85000m);
        await SeedAndEmbedAsync(category.Id, "Running Shoes", "Lightweight trainers with cushioned soles for road running.", 7000m);
        return kettle;
    }

    [Fact]
    public async Task Consumer_StoresRealEmbeddings()
    {
        var kettle = await SeedCatalogAsync();

        await using var context = _db.CreateContext();
        var stored = await context.Products.AsNoTracking().SingleAsync(p => p.Id == kettle);
        Assert.Equal(new AiOptions().EmbeddingDimensions, stored.Embedding!.ToArray().Length);
        Assert.NotNull(stored.EmbeddingGeneratedAt);
    }

    [Fact]
    public async Task SemanticSearch_RanksTheRelevantProductFirst()
    {
        var kettle = await SeedCatalogAsync();

        await using var context = _db.CreateContext();
        var results = await new SemanticSearchQueryHandler(context, Embeddings)
            .Handle(new SemanticSearchQuery("something to boil water for tea"), CancellationToken.None);

        Assert.Equal(3, results.Count);
        Assert.Equal(kettle, results[0].ProductId);
        Assert.True(results[0].SimilarityScore > results[1].SimilarityScore);
    }

    [Fact]
    public async Task Ask_AnswersFromTheRelevantProduct()
    {
        var kettle = await SeedCatalogAsync();

        await using var context = _db.CreateContext();
        var result = await new AskProductsQueryHandler(
                context, Embeddings, _ai.GetRequiredService<ILlmProviderFactory>(), new FakeCurrentUser())
            .Handle(new AskProductsQuery("I need something to boil water for tea"), CancellationToken.None);

        Assert.NotEmpty(result.Sources);
        Assert.Equal(kettle, result.Sources[0].ProductId);
        Assert.False(string.IsNullOrWhiteSpace(result.Answer));
        Assert.True(result.InputTokens > 0);
        Assert.True(result.OutputTokens > 0);
    }
}
