using Ai.Contracts;
using Ai.Infrastructure;
using Ai.Infrastructure.Ollama;
using Ai.Infrastructure.OpenAi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ai.UnitTests.Infrastructure;

public class DependencyInjectionTests
{
    private static ServiceProvider Build(Dictionary<string, string?> settings) =>
        new ServiceCollection()
            .AddAi(new ConfigurationBuilder().AddInMemoryCollection(settings).Build())
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = false, ValidateScopes = true });

    [Fact]
    public void BindsAiSection()
    {
        using var provider = Build(new()
        {
            ["Ai:DefaultChatProvider"] = "OpenAI",
            ["Ai:OllamaBaseUrl"] = "http://localhost:11434",
            ["Ai:ChatMaxTokens"] = "300",
        });

        var options = provider.GetRequiredService<IOptions<AiOptions>>().Value;
        Assert.Equal("OpenAI", options.DefaultChatProvider);
        Assert.Equal("http://localhost:11434", options.OllamaBaseUrl);
        Assert.Equal(300, options.ChatMaxTokens);
    }

    [Fact]
    public void EmbeddingService_IsOllama()
    {
        using var provider = Build([]);

        Assert.IsType<OllamaEmbeddingService>(provider.GetRequiredService<IEmbeddingService>());
    }

    /// <summary>Unwraps the metrics decorator every chat provider is registered behind.</summary>
    private static IChatCompletionService Unwrap(IChatCompletionService service) =>
        Assert.IsType<MeteredChatCompletionService>(service).Inner;

    [Fact]
    public void Factory_DefaultsToOllama()
    {
        using var provider = Build([]);

        Assert.IsType<OllamaChatCompletionService>(Unwrap(provider.GetRequiredService<ILlmProviderFactory>().GetChatService()));
    }

    [Fact]
    public void Factory_ResolvesOpenAi_WhenKeyConfigured()
    {
        using var provider = Build(new() { ["Ai:OpenAiApiKey"] = "sk-test" });

        Assert.IsType<OpenAiChatCompletionService>(Unwrap(provider.GetRequiredService<ILlmProviderFactory>().GetChatService("OpenAI")));
    }

    [Theory]
    [InlineData("Ollama")]
    [InlineData("OpenAI")]
    public void EveryChatProvider_IsMetered(string providerName)
    {
        using var provider = Build(new() { ["Ai:OpenAiApiKey"] = "sk-test" });

        Assert.IsType<MeteredChatCompletionService>(provider.GetRequiredService<ILlmProviderFactory>().GetChatService(providerName));
    }

    [Fact]
    public void ChatProviders_AreSingletons()
    {
        using var provider = Build([]);
        var factory = provider.GetRequiredService<ILlmProviderFactory>();

        Assert.Same(factory.GetChatService("Ollama"), factory.GetChatService("Ollama"));
    }

    /// <summary>A missing OpenAI key only fails when OpenAI is actually requested, so Ollama-only setups still start.</summary>
    [Fact]
    public void Factory_OpenAiWithoutKey_FailsOnResolve_NotOnStartup()
    {
        using var provider = Build([]);
        var factory = provider.GetRequiredService<ILlmProviderFactory>();

        Assert.IsType<OllamaChatCompletionService>(Unwrap(factory.GetChatService("Ollama")));
        var ex = Assert.Throws<InvalidOperationException>(() => factory.GetChatService("OpenAI"));
        Assert.Contains("Ai__OpenAiApiKey", ex.Message);
    }
}
