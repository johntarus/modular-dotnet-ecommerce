using Ai.Contracts;
using Ai.Contracts.Models;
using BuildingBlocks.Infrastructure.CurrentUser;

namespace Catalog.IntegrationTests.Infrastructure;

/// <summary>Builds 1024-dim vectors whose first components are given; the rest are zero.</summary>
public static class Vectors
{
    public const int Dimensions = 1024;

    public static float[] Of(params float[] head)
    {
        var v = new float[Dimensions];
        head.CopyTo(v, 0);
        return v;
    }
}

public sealed class FakeEmbeddingService(Func<string, float[]> embed) : IEmbeddingService
{
    public FakeEmbeddingService(float[] vector) : this(_ => vector) { }

    public List<string> Inputs { get; } = [];

    public Task<EmbeddingResponse> EmbedAsync(string text, CancellationToken ct = default)
    {
        Inputs.Add(text);
        var vector = embed(text);
        return Task.FromResult(new EmbeddingResponse(vector, vector.Length, 0, "fake-embed"));
    }

    public async Task<IReadOnlyList<EmbeddingResponse>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken ct = default)
    {
        var results = new List<EmbeddingResponse>();
        foreach (var text in texts) results.Add(await EmbedAsync(text, ct));
        return results;
    }
}

public sealed class FakeChatService(string content = "Try the Kettle at KES 2,500.", int input = 120, int output = 18)
    : IChatCompletionService
{
    public List<IReadOnlyList<ChatMessage>> Calls { get; } = [];

    public Task<ChatResponse> CompleteAsync(IReadOnlyList<ChatMessage> messages, CancellationToken ct = default)
    {
        Calls.Add(messages);
        return Task.FromResult(new ChatResponse(content, input, output, "fake-chat"));
    }
}

public sealed class FakeLlmProviderFactory(FakeChatService chat) : ILlmProviderFactory
{
    public FakeChatService Chat => chat;

    public List<string?> RequestedProviders { get; } = [];

    public IChatCompletionService GetChatService(string? providerName = null)
    {
        RequestedProviders.Add(providerName);
        return chat;
    }
}

public sealed class FakeCurrentUser(Guid? userId = null) : ICurrentUser
{
    public Guid? UserId { get; } = userId;
}
