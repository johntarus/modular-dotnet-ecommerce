using Ai.Contracts;
using Ai.Contracts.Models;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;

namespace Ai.Infrastructure.Ollama;

internal sealed class OllamaEmbeddingService : IEmbeddingService
{
    private readonly OllamaApiClient _client;
    private readonly AiOptions _options;

    public OllamaEmbeddingService(IOptions<AiOptions> options)
    {
        _options = options.Value;
        _client = new OllamaApiClient(new Uri(_options.OllamaBaseUrl));
    }

    public async Task<EmbeddingResponse> EmbedAsync(string text, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
    
        var request = new EmbedRequest
        {
            Model = _options.OllamaEmbeddingModel,
            Input = new List<string> { text }
        };

        var response = await _client.EmbedAsync(request, ct);
    
        var floats = response.Embeddings.First().Select(d => d).ToArray();

        return new EmbeddingResponse(
            Vector: floats,
            Dimensions: floats.Length,
            InputTokens: 0,
            Model: _options.OllamaEmbeddingModel);
    }

    public async Task<IReadOnlyList<EmbeddingResponse>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken ct = default)
    {
        var list = texts.ToList();
        if (list.Count == 0) return Array.Empty<EmbeddingResponse>();

        // Ollama's EmbedAsync can handle multiple inputs at once
        var request = new EmbedRequest
        {
            Model = _options.OllamaEmbeddingModel,
            Input = list
        };

        var response = await _client.EmbedAsync(request, ct);

        return response.Embeddings.Select(embedding => new EmbeddingResponse(
            Vector: embedding.Select(d => d).ToArray(),
            Dimensions: embedding.Length,
            InputTokens: 0,
            Model: _options.OllamaEmbeddingModel
        )).ToList();
    }
}