using Ai.Contracts;
using BuildingBlocks.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ai.Infrastructure;

public class LlmProviderFactory(IServiceProvider serviceProvider, IOptions<AiOptions> options) : ILlmProviderFactory
{
    public IChatCompletionService GetChatService(string? providerName = null)
    {
        // A blank name (e.g. "" from a form or Swagger) means "use the configured default".
        if (string.IsNullOrWhiteSpace(providerName))
            return Resolve(LlmProviders.Normalize(options.Value.DefaultChatProvider)
                ?? throw new InvalidOperationException(
                    $"Ai:DefaultChatProvider is set to '{options.Value.DefaultChatProvider}', which is not supported. " +
                    $"Use one of: {string.Join(", ", LlmProviders.All)}."));

        // A name the caller chose is a client error (400), not a server error.
        return Resolve(LlmProviders.Normalize(providerName)
            ?? throw new ValidationException(LlmProviders.UnknownProviderMessage(providerName)));
    }

    private IChatCompletionService Resolve(string provider) =>
        serviceProvider.GetRequiredKeyedService<IChatCompletionService>(provider);
}
