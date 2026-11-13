using Ai.Contracts;
using Microsoft.Extensions.Options;
using OpenAI;

namespace Ai.Infrastructure.OpenAi;

/// <summary>
/// Creates a single OpenAI SDK client per process. The SDK manages
/// its own HttpClient pool, so we only build it once.
/// </summary>
internal sealed class OpenAiClientFactory
{
    private readonly OpenAIClient _client;

    public OpenAiClientFactory(IOptions<AiOptions> options)
    {
        if (string.IsNullOrWhiteSpace(options.Value.OpenAiApiKey))
            throw new InvalidOperationException(
                "Ai:ApiKey is not configured. Set Ai__ApiKey environment variable or add it to appsettings.");

        _client = new OpenAIClient(options.Value.OpenAiApiKey);
    }

    public OpenAIClient Client => _client;
}