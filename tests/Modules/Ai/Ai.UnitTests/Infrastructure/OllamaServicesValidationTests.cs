using Ai.Contracts;
using Ai.Contracts.Models;
using Ai.Infrastructure.Ollama;
using Microsoft.Extensions.Options;

namespace Ai.UnitTests.Infrastructure;

/// <summary>
/// Guards that reject bad input before any request is sent. The base URL points at a port nothing
/// listens on, so a test that reached the network would fail with a connection error instead.
/// Behaviour against a real Ollama is covered in Ai.IntegrationTests.
/// </summary>
public class OllamaServicesValidationTests
{
    private static readonly IOptions<AiOptions> Unreachable = Options.Create(new AiOptions { OllamaBaseUrl = "http://127.0.0.1:9" });

    [Fact]
    public async Task Chat_NoMessages_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => new OllamaChatCompletionService(Unreachable).CompleteAsync([]));
    }

    [Fact]
    public async Task Chat_NullMessages_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => new OllamaChatCompletionService(Unreachable).CompleteAsync(null!));
    }

    [Fact]
    public async Task Chat_UnknownRole_Throws()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => new OllamaChatCompletionService(Unreachable).CompleteAsync([new("tool", "x")]));

        Assert.Contains("tool", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Embed_BlankText_Throws(string text)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => new OllamaEmbeddingService(Unreachable).EmbedAsync(text));
    }

    [Fact]
    public async Task EmbedBatch_Empty_ReturnsEmpty()
    {
        Assert.Empty(await new OllamaEmbeddingService(Unreachable).EmbedBatchAsync([]));
    }
}
