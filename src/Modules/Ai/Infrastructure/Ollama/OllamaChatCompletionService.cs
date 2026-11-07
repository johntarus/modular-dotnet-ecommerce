using Ai.Contracts;
using Ai.Contracts.Models;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models.Chat;
using ChatMessage = Ai.Contracts.Models.ChatMessage;

namespace Ai.Infrastructure.Ollama;

internal sealed class OllamaChatCompletionService : IChatCompletionService
{
    private readonly OllamaApiClient _client;
    private readonly AiOptions _options;

    public OllamaChatCompletionService(IOptions<AiOptions> options)
    {
        _options = options.Value;
        _client = new OllamaApiClient(new Uri(_options.OllamaBaseUrl));
    }

    public async Task<ChatResponse> CompleteAsync(IReadOnlyList<ChatMessage> messages, CancellationToken ct = default)
    {
        var sdkMessages = messages.Select(m => new OllamaSharp.Models.Chat.Message
        {
            Role = m.Role switch
            {
                "system" => ChatRole.System,
                "user" => ChatRole.User,
                "assistant" => ChatRole.Assistant,
                _ => throw new ArgumentException($"Unknown role: {m.Role}")
            },
            Content = m.Content
        }).ToList();

        var request = new ChatRequest
        {
            Model = _options.ChatModel,
            Messages = sdkMessages,
            // Fully qualified to avoid conflict with OpenAI's RequestOptions
            Options = new OllamaSharp.Models.RequestOptions
            {
                Temperature = 0.2f 
            }
        };

        var response = new System.Text.StringBuilder();
        await foreach (var stream in _client.ChatAsync(request, ct))
        {
            response.Append(stream?.Message?.Content);
        }

        return new ChatResponse(
            Content: response.ToString(),
            InputTokens: 0,
            OutputTokens: 0,
            Model: _options.ChatModel);
    }
}