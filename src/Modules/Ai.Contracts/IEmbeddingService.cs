using Ai.Contracts.Models;

namespace Ai.Contracts;

public interface IEmbeddingService
{
    /// <summary>
    /// Generate a vector embedding for the given text.
    /// </summary>
    Task<EmbeddingResponse> EmbedAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Generate embeddings for multiple texts in a single batch (cheaper, faster).
    /// </summary>
    Task<IReadOnlyList<EmbeddingResponse>> EmbedBatchAsync(
        IEnumerable<string> texts, CancellationToken ct = default);
}

