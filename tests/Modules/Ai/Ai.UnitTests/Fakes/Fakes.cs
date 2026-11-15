using Ai.Contracts;
using Ai.Contracts.Models;

namespace Ai.UnitTests.Fakes;

internal sealed class FakeChatService(Func<IReadOnlyList<ChatMessage>, ChatResponse> reply) : IChatCompletionService
{
    public FakeChatService(string content = "ok", int input = 0, int output = 0, string model = "fake")
        : this(_ => new ChatResponse(content, input, output, model)) { }

    public List<IReadOnlyList<ChatMessage>> Calls { get; } = [];

    public Task<ChatResponse> CompleteAsync(IReadOnlyList<ChatMessage> messages, CancellationToken ct = default)
    {
        Calls.Add(messages);
        return Task.FromResult(reply(messages));
    }
}
