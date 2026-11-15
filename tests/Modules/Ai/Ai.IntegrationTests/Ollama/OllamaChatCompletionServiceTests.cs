using Ai.Contracts;
using Ai.Contracts.Models;
using Ai.Infrastructure.Ollama;
using Ai.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Options;

namespace Ai.IntegrationTests.Ollama;

[Collection(OllamaCollection.Name)]
public class OllamaChatCompletionServiceTests(OllamaFixture ollama)
{
    private readonly OllamaChatCompletionService _service = new(Options.Create(ollama.Options));

    private static readonly ChatMessage[] Ping =
    [
        ChatMessage.System("You are a test endpoint. Reply with exactly one word: PONG."),
        ChatMessage.User("PING"),
    ];

    [Fact]
    public async Task ReturnsTrimmedReply()
    {
        var response = await _service.CompleteAsync(Ping);

        Assert.Contains("PONG", response.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(response.Content.Trim(), response.Content);
        Assert.Equal(OllamaFixture.ChatModel, response.Model);
    }

    /// <summary>
    /// Token counts are read by reflection from the final streamed chunk, so a change in
    /// OllamaSharp would silently turn them into zeros. Real traffic catches that.
    /// </summary>
    [Fact]
    public async Task ReportsRealTokenCounts()
    {
        var response = await _service.CompleteAsync(Ping);

        Assert.True(response.InputTokens > 0, $"InputTokens was {response.InputTokens}");
        Assert.True(response.OutputTokens > 0, $"OutputTokens was {response.OutputTokens}");
    }

    [Fact]
    public async Task UsesConversationHistory()
    {
        var response = await _service.CompleteAsync(
        [
            ChatMessage.System("Answer with a single word."),
            ChatMessage.User("My favourite colour is turquoise. Remember it."),
            ChatMessage.Assistant("Okay."),
            ChatMessage.User("What is my favourite colour?"),
        ]);

        Assert.Contains("turquoise", response.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.CompleteAsync(Ping, cts.Token));
    }

    [Fact]
    public async Task UnknownModel_Throws()
    {
        var service = new OllamaChatCompletionService(Options.Create(new AiOptions
        {
            OllamaBaseUrl = ollama.BaseUrl,
            OllamaChatModel = "no-such-model",
        }));

        await Assert.ThrowsAnyAsync<Exception>(() => service.CompleteAsync(Ping));
    }
}
