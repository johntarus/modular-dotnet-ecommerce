using Ai.Contracts;
using Ai.Contracts.Models;
using Ai.Infrastructure.OpenAi;
using Microsoft.Extensions.Options;

namespace Ai.UnitTests.Infrastructure;

/// <summary>Covers the guards that run before any request leaves the process.</summary>
public class OpenAiServicesTests
{
    private static readonly IOptions<AiOptions> Configured = Options.Create(new AiOptions { OpenAiApiKey = "sk-test" });

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ClientFactory_WithoutApiKey_Throws(string key)
    {
        Assert.Throws<InvalidOperationException>(() => new OpenAiClientFactory(Options.Create(new AiOptions { OpenAiApiKey = key })));
    }

    [Fact]
    public async Task Chat_UnknownRole_Throws()
    {
        var service = new OpenAiChatCompletionService(new OpenAiClientFactory(Configured), Configured);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CompleteAsync([new("tool", "x")]));

        Assert.Contains("tool", ex.Message);
    }

    [Fact]
    public async Task EmbedAsync_BlankText_Throws()
    {
        var service = new OpenAiEmbeddingService(new OpenAiClientFactory(Configured), Configured);

        await Assert.ThrowsAsync<ArgumentException>(() => service.EmbedAsync(" "));
    }

    [Fact]
    public async Task EmbedBatchAsync_BlankItem_ThrowsNamingItsIndex()
    {
        var service = new OpenAiEmbeddingService(new OpenAiClientFactory(Configured), Configured);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.EmbedBatchAsync(["mug", "kettle", ""]));

        Assert.Contains("index 2", ex.Message);
    }

    [Fact]
    public async Task EmbedBatchAsync_Empty_ReturnsEmpty()
    {
        var service = new OpenAiEmbeddingService(new OpenAiClientFactory(Configured), Configured);

        Assert.Empty(await service.EmbedBatchAsync([]));
    }
}
