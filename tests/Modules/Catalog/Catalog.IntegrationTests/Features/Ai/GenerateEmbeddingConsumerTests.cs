using Catalog.Domain.Events;
using Catalog.Features.Products.EventHandlers;
using Catalog.Features.Products.UpdateProduct;
using Catalog.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace Catalog.IntegrationTests.Features.Ai;

public class GenerateEmbeddingConsumerTests(PostgresFixture db) : AiTestBase(db)
{
    private readonly FakeEmbeddingService _embeddings = new(Vectors.Of(0.6f, 0.8f));

    private async Task RunAsync(Guid productId)
    {
        await using var context = NewContext();
        await new GenerateEmbeddingConsumer(context, _embeddings, NullLogger<GenerateEmbeddingConsumer>.Instance)
            .GenerateEmbeddingAsync(productId, CancellationToken.None);
    }

    [Fact]
    public async Task NewProduct_EmbedsNameAndDescription_AndPersistsVector()
    {
        var categoryId = await SeedCategoryAsync();
        var id = await SeedProductAsync(categoryId, name: "Kettle");

        await RunAsync(id);

        Assert.Equal("Kettle. Description", Assert.Single(_embeddings.Inputs));
        var stored = (await LoadProductAsync(id))!;
        Assert.NotNull(stored.EmbeddingGeneratedAt);
        var vector = stored.Embedding!.ToArray();
        Assert.Equal(Vectors.Dimensions, vector.Length);
        Assert.Equal(0.6f, vector[0], 5);
        Assert.Equal(0.8f, vector[1], 5);
    }

    [Fact]
    public async Task DispatchesEmbeddingGeneratedEvent()
    {
        var categoryId = await SeedCategoryAsync();
        var id = await SeedProductAsync(categoryId);

        await RunAsync(id);

        var generated = Assert.IsType<ProductEmbeddingGeneratedDomainEvent>(Assert.Single(DomainEvents.Published));
        Assert.Equal(id, generated.ProductId);
    }

    [Fact]
    public async Task MissingProduct_DoesNothing()
    {
        await RunAsync(Guid.NewGuid());

        Assert.Empty(_embeddings.Inputs);
    }

    [Fact]
    public async Task DeletedProduct_IsNotEmbedded()
    {
        var categoryId = await SeedCategoryAsync();
        var id = await SeedProductAsync(categoryId, deleted: true);

        await RunAsync(id);

        Assert.Empty(_embeddings.Inputs);
        Assert.Null((await LoadProductAsync(id))!.Embedding);
    }

    /// <summary>MassTransit redelivers messages, so a repeat must not pay for a second embedding call.</summary>
    [Fact]
    public async Task FreshEmbedding_IsSkipped()
    {
        var categoryId = await SeedCategoryAsync();
        var id = await SeedProductAsync(categoryId);
        await RunAsync(id);
        var firstGeneratedAt = (await LoadProductAsync(id))!.EmbeddingGeneratedAt;

        await RunAsync(id);

        Assert.Single(_embeddings.Inputs);
        Assert.Equal(firstGeneratedAt, (await LoadProductAsync(id))!.EmbeddingGeneratedAt);
    }

    [Fact]
    public async Task TextChange_ClearsEmbedding_AndNextRunReembedsNewText()
    {
        var categoryId = await SeedCategoryAsync();
        var id = await SeedProductAsync(categoryId, name: "Kettle");
        await RunAsync(id);

        await using (var context = NewContext())
        {
            await new UpdateProductCommandHandler(context).Handle(
                new UpdateProductCommand("Glass kettle", "Borosilicate", 10m, categoryId) { Id = id }, CancellationToken.None);
        }
        Assert.Null((await LoadProductAsync(id))!.Embedding);

        await RunAsync(id);

        Assert.Equal(["Kettle. Description", "Glass kettle. Borosilicate"], _embeddings.Inputs);
        Assert.NotNull((await LoadProductAsync(id))!.Embedding);
    }

    /// <summary>The failure must reach MassTransit so it retries; nothing half-written is saved.</summary>
    [Fact]
    public async Task EmbeddingFailure_Propagates_AndLeavesProductUnembedded()
    {
        var categoryId = await SeedCategoryAsync();
        var id = await SeedProductAsync(categoryId);
        var failing = new FakeEmbeddingService(_ => throw new HttpRequestException("ollama down"));

        await using var context = NewContext();
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            new GenerateEmbeddingConsumer(context, failing, NullLogger<GenerateEmbeddingConsumer>.Instance)
                .GenerateEmbeddingAsync(id, CancellationToken.None));

        Assert.Null((await LoadProductAsync(id))!.Embedding);
    }

    /// <summary>Ollama returns whatever its model produces; a model swap must not corrupt the column.</summary>
    [Fact]
    public async Task WrongDimensionVector_IsRejected_AndNothingSaved()
    {
        var categoryId = await SeedCategoryAsync();
        var id = await SeedProductAsync(categoryId);
        var wrongModel = new FakeEmbeddingService(new float[768]);

        await using var context = NewContext();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new GenerateEmbeddingConsumer(context, wrongModel, NullLogger<GenerateEmbeddingConsumer>.Instance)
                .GenerateEmbeddingAsync(id, CancellationToken.None));

        Assert.Null((await LoadProductAsync(id))!.Embedding);
    }
}
