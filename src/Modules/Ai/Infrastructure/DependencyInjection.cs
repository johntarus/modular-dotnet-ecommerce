using Ai.Contracts;
using Ai.Infrastructure.Ollama;
using Ai.Infrastructure.OpenAi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ai.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAi(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AiOptions>(config.GetSection(AiOptions.SectionName));

        var provider = config[$"{AiOptions.SectionName}:Provider"] ?? "Ollama";

        if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<OpenAiClientFactory>();
            services.AddSingleton<IEmbeddingService, OpenAiEmbeddingService>();
            services.AddSingleton<IChatCompletionService, OpenAiChatCompletionService>();
        }
        else if (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();
            services.AddSingleton<IChatCompletionService, OllamaChatCompletionService>();
        }
        else
        {
            throw new InvalidOperationException($"Unsupported AI provider: {provider}");
        }

        return services;
    }
}