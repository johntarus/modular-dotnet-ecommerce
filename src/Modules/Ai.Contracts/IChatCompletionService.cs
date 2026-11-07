using Ai.Contracts.Models;

namespace Ai.Contracts;

/// <summary>
/// Sends messages to a chat LLM and returns a generated reply.
/// Used for RAG pipelines, question answering, summarization, etc.
/// </summary>
public interface IChatCompletionService
{
    Task<ChatResponse> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct = default);
}