using Ai.Contracts;
using Ai.Infrastructure;
using Ai.UnitTests.Fakes;
using BuildingBlocks.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ai.UnitTests.Infrastructure;

public class LlmProviderFactoryTests
{
    private readonly FakeChatService _ollama = new("from ollama");
    private readonly FakeChatService _openAi = new("from openai");

    private LlmProviderFactory CreateFactory(string defaultProvider = "Ollama")
    {
        var provider = new ServiceCollection()
            .AddKeyedSingleton<IChatCompletionService>("Ollama", _ollama)
            .AddKeyedSingleton<IChatCompletionService>("OpenAI", _openAi)
            .BuildServiceProvider();

        return new LlmProviderFactory(provider, Options.Create(new AiOptions { DefaultChatProvider = defaultProvider }));
    }

    [Fact]
    public void NoProviderName_UsesConfiguredDefault()
    {
        Assert.Same(_openAi, CreateFactory(defaultProvider: "OpenAI").GetChatService());
    }

    [Fact]
    public void ExplicitProviderName_OverridesDefault()
    {
        Assert.Same(_openAi, CreateFactory(defaultProvider: "Ollama").GetChatService("OpenAI"));
    }

    [Theory]
    [InlineData("openai")]
    [InlineData("OPENAI")]
    [InlineData(" OpenAI ")]
    public void ProviderName_IgnoresCaseAndSurroundingSpaces(string name)
    {
        Assert.Same(_openAi, CreateFactory().GetChatService(name));
    }

    /// <summary>Swagger and forms send "" for an untouched field; that means "use the default".</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankProviderName_UsesConfiguredDefault(string name)
    {
        Assert.Same(_ollama, CreateFactory(defaultProvider: "Ollama").GetChatService(name));
    }

    [Fact]
    public void ConfiguredDefault_IgnoresCase()
    {
        Assert.Same(_openAi, CreateFactory(defaultProvider: "openai").GetChatService());
    }

    /// <summary>A bad name from the caller maps to 400 with a message saying what is allowed.</summary>
    [Fact]
    public void UnknownProviderName_ThrowsValidationException_ListingSupportedProviders()
    {
        var ex = Assert.Throws<ValidationException>(() => CreateFactory().GetChatService("Gemini"));

        Assert.Equal("Unknown AI provider 'Gemini'. Supported providers: Ollama, OpenAI.", Assert.Single(ex.Errors));
    }

    /// <summary>A bad default is a deployment mistake, not the caller's, so it stays a server error.</summary>
    [Fact]
    public void UnsupportedConfiguredDefault_ThrowsInvalidOperation_NamingTheSetting()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => CreateFactory(defaultProvider: "Gemini").GetChatService());

        Assert.Contains("Ai:DefaultChatProvider", ex.Message);
        Assert.Contains("Gemini", ex.Message);
    }
}
