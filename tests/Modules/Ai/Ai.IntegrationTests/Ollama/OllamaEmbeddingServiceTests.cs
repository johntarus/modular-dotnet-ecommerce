using Ai.Contracts;
using Ai.Infrastructure.Ollama;
using Ai.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Options;

namespace Ai.IntegrationTests.Ollama;

[Collection(OllamaCollection.Name)]
public class OllamaEmbeddingServiceTests(OllamaFixture ollama)
{
    private const string QueryPrefix = "Represent this sentence for searching relevant passages: ";

    private readonly OllamaEmbeddingService _service = new(Options.Create(ollama.Options));

    private static double Cosine(float[] a, float[] b)
    {
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < a.Length; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    /// <summary>The products.Embedding column and Product.SetEmbedding both require this size.</summary>
    [Fact]
    public async Task ProducesVectorsOfTheConfiguredDimension()
    {
        var response = await _service.EmbedAsync("Electric kettle. 1.7L stainless steel");

        Assert.Equal(new AiOptions().EmbeddingDimensions, response.Dimensions);
        Assert.Equal(response.Dimensions, response.Vector.Length);
        Assert.Equal(OllamaFixture.EmbeddingModel, response.Model);
    }

    [Fact]
    public async Task SameText_GivesSameVector()
    {
        var first = await _service.EmbedAsync("Ceramic coffee mug");
        var second = await _service.EmbedAsync("Ceramic coffee mug");

        Assert.True(Cosine(first.Vector, second.Vector) > 0.9999);
    }

    [Fact]
    public async Task Batch_ReturnsOneVectorPerText_InInputOrder()
    {
        string[] texts = ["Electric kettle", "Leather sofa", "Running shoes"];

        var batch = await _service.EmbedBatchAsync(texts);

        Assert.Equal(texts.Length, batch.Count);
        for (var i = 0; i < texts.Length; i++)
        {
            var single = await _service.EmbedAsync(texts[i]);
            Assert.True(Cosine(single.Vector, batch[i].Vector) > 0.999, $"batch[{i}] does not match '{texts[i]}'");
        }
    }

    /// <summary>The retrieval the Catalog relies on: a prefixed query lands nearest the matching product.</summary>
    [Fact]
    public async Task PrefixedQuery_IsClosestToTheRelevantProduct()
    {
        var query = await _service.EmbedAsync(QueryPrefix + "something to boil water for tea");
        var kettle = await _service.EmbedAsync("Electric kettle. 1.7L stainless steel, boils water in minutes.");
        var sofa = await _service.EmbedAsync("Leather sofa. Three-seater in dark brown leather.");

        Assert.True(Cosine(query.Vector, kettle.Vector) > Cosine(query.Vector, sofa.Vector));
    }

    [Fact]
    public async Task UnknownModel_Throws()
    {
        var service = new OllamaEmbeddingService(Options.Create(new AiOptions
        {
            OllamaBaseUrl = ollama.BaseUrl,
            OllamaEmbeddingModel = "no-such-model",
        }));

        await Assert.ThrowsAnyAsync<Exception>(() => service.EmbedAsync("kettle"));
    }
}
