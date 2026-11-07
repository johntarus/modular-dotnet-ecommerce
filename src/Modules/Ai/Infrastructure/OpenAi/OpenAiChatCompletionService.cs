using Ai.Contracts;
using Ai.Contracts.Models;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Ai.Contracts.Models;

namespace Ai.Infrastructure.OpenAi;

internal sealed class OpenAiChatCompletionService(
    OpenAiClientFactory clientFactory,
    IOptions<AiOptions> options) : IChatCompletionService
{
    private readonly ChatClient _chat = clientFactory.Client.GetChatClient(options.Value.ChatModel);

    public async Task<ChatResponse> CompleteAsync(
        IReadOnlyList<Contracts.Models.ChatMessage> messages,
        CancellationToken ct = default)
    {
        // Map OUR ChatMessage → SDK ChatMessage
        var sdkMessages = messages.Select<Contracts.Models.ChatMessage, OpenAI.Chat.ChatMessage>(m =>
            m.Role.ToLowerInvariant() switch
            {
                "system"    => new SystemChatMessage(m.Content),
                "user"      => new UserChatMessage(m.Content),
                "assistant" => new AssistantChatMessage(m.Content),
                _ => throw new ArgumentException(
                    $"Unknown chat role '{m.Role}'. Expected: system | user | assistant.",
                    nameof(messages))
            }).ToList();

        var completionOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = options.Value.ChatMaxTokens,
            Temperature = 0.2f   // lower = more grounded, less creative (good for RAG)
        };

        var response = await _chat.CompleteChatAsync(sdkMessages, completionOptions, ct);

        var content = string.Join("", response.Value.Content.Select(c => c.Text));

        return new ChatResponse(
            Content: content,
            InputTokens: response.Value.Usage.InputTokenCount,
            OutputTokens: response.Value.Usage.OutputTokenCount,
            Model: options.Value.ChatModel);
    }
}