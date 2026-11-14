namespace Ai.Contracts;

public interface ILlmProviderFactory
{
    IChatCompletionService GetChatService(string? providerName = null);
}