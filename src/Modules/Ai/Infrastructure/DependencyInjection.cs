using Ai.Contracts;
using Ai.Infrastructure.OpenAi;
using Ai.Infrastructure.Ollama;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ai.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAi(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AiOptions>(config.GetSection(AiOptions.SectionName));

        // Needed by the OpenAI chat service
        services.AddSingleton<OpenAiClientFactory>();

        // ── 1. EMBEDDING (Global Singleton) ──
        // We do NOT use Keyed Services here. 
        // It is registered once, and injected as a plain IEmbeddingService.
        services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();

        // ── 2. CHAT (Keyed Services / Strategy Pattern) ──
        // Every provider is wrapped in MeteredChatCompletionService so token usage,
        // latency and failures are recorded no matter which one the factory picks.
        services.AddKeyedSingleton<IChatCompletionService>(LlmProviders.Ollama, (sp, _) =>
            new MeteredChatCompletionService(ActivatorUtilities.CreateInstance<OllamaChatCompletionService>(sp)));
        services.AddKeyedSingleton<IChatCompletionService>(LlmProviders.OpenAI, (sp, _) =>
            new MeteredChatCompletionService(ActivatorUtilities.CreateInstance<OpenAiChatCompletionService>(sp)));

        // ── 3. FACTORY ──
        services.AddSingleton<ILlmProviderFactory, LlmProviderFactory>();

        return services;
    }
}