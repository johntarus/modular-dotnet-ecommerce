using Ai.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ai.Infrastructure;

public class LlmProviderFactory(IServiceProvider serviceProvider, IOptions<AiOptions> options) : ILlmProviderFactory
{
    public IChatCompletionService GetChatService(string? providerName = null)
    {
        // If the request didn't specify, fall back to the global config
        var provider = providerName ?? options.Value.DefaultChatProvider;
        
        // Resolve using .NET Keyed Services
        return serviceProvider.GetRequiredKeyedService<IChatCompletionService>(provider);
    }
}