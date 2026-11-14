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

        // FIX: Register the OpenAI Client Factory (needed by the Chat Strategy)
        services.AddSingleton<OpenAiClientFactory>();

        // ── 1. EMBEDDING (Global Singleton) ──
        // We do NOT use Keyed Services here. 
        // It is registered once, and injected as a plain IEmbeddingService.
        services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();

        // ── 2. CHAT (Keyed Services / Strategy Pattern) ──
        services.AddKeyedSingleton<IChatCompletionService, OllamaChatCompletionService>("Ollama");
        services.AddKeyedSingleton<IChatCompletionService, OpenAiChatCompletionService>("OpenAI");

        // ── 3. FACTORY ──
        services.AddSingleton<ILlmProviderFactory, LlmProviderFactory>();

        return services;
    }
}