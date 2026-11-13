using System.Text;
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

    public async Task<ChatResponse> CompleteAsync(
        IReadOnlyList<ChatMessage> messages, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        if (messages.Count == 0)
            throw new ArgumentException("At least one message is required.", nameof(messages));

        var sdkMessages = messages.Select(m => new Message
        {
            Role = m.Role.ToLowerInvariant() switch
            {
                "system" => ChatRole.System,
                "user" => ChatRole.User,
                "assistant" => ChatRole.Assistant,
                _ => throw new ArgumentException($"Unknown role '{m.Role}'")
            },
            Content = m.Content
        }).ToList();

        var request = new ChatRequest
        {
            Model = _options.OllamaChatModel,
            Messages = sdkMessages,
            Stream = true,
            Options = new OllamaSharp.Models.RequestOptions { Temperature = 0.2f }
        };

        var content = new StringBuilder();
        int inputTokens = 0;
        int outputTokens = 0;

        await foreach (var chunk in _client.ChatAsync(request, ct))
        {
            if (chunk?.Message?.Content is not null)
                content.Append(chunk.Message.Content);

            if (chunk?.Done == true)
            {
                var t = chunk.GetType();
                var pe = t.GetProperty("PromptEvalCount")?.GetValue(chunk);
                var e = t.GetProperty("EvalCount")?.GetValue(chunk);
                if (pe is int pi) inputTokens = pi;
                if (e is int ei) outputTokens = ei;
            }
        }

        return new ChatResponse(
            Content: content.ToString().Trim(),
            InputTokens: inputTokens,
            OutputTokens: outputTokens,
            Model: _options.OllamaChatModel);
    }
}