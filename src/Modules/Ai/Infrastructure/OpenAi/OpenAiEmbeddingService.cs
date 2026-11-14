using Ai.Contracts;
using Ai.Contracts.Models;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace Ai.Infrastructure.OpenAi;

internal sealed class OpenAiEmbeddingService(
    OpenAiClientFactory clientFactory,
    IOptions<AiOptions> options) : IEmbeddingService
{
    private readonly EmbeddingClient _embeddings =
        clientFactory.Client.GetEmbeddingClient(options.Value.OpenAiEmbeddingModel);

    public async Task<EmbeddingResponse> EmbedAsync(string text, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        // Delegate to batch — the batch API returns Usage + Model,
        // the single-embedding API doesn't expose them.
        var results = await EmbedBatchAsync(new[] { text }, ct);
        return results[0];
    }

    public async Task<IReadOnlyList<EmbeddingResponse>> EmbedBatchAsync(
        IEnumerable<string> texts,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(texts);

        // Validate before the API call — one bad input poisons the batch
        var list = texts
            .Select((t, i) => (Text: t, Index: i))
            .Select(x =>
            {
                if (string.IsNullOrWhiteSpace(x.Text))
                    throw new ArgumentException(
                        $"Embedding text at index {x.Index} is null or whitespace.",
                        nameof(texts));
                return x.Text;
            })
            .ToList();

        if (list.Count == 0) return Array.Empty<EmbeddingResponse>();

        var responses = new List<EmbeddingResponse>(list.Count);

        const int batchSize = 100;
        for (int i = 0; i < list.Count; i += batchSize)
        {
            var batch = list.Skip(i).Take(batchSize).ToList();
            var result = await _embeddings.GenerateEmbeddingsAsync(batch, cancellationToken: ct);

            var perItemTokens = batch.Count > 0
                ? result.Value.Usage.InputTokenCount / batch.Count
                : 0;

            var model = result.Value.Model;   // ← available on EmbeddingCollection

            // EmbeddingCollection is IEnumerable<Embedding> — .Select works directly
            responses.AddRange(result.Value.Select(e =>
            {
                var floats = e.ToFloats().ToArray();
                return new EmbeddingResponse(
                    Vector: floats,
                    Dimensions: floats.Length,
                    InputTokens: perItemTokens,
                    Model: model);
            }));
        }

        return responses;
    }
}