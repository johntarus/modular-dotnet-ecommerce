namespace Ai.Contracts;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string DefaultProvider { get; set; } = "Ollama";   // "OpenAI" | "Ollama" | "Azure"
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    public int EmbeddingDimensions { get; set; } = 1024;
    public int ChatMaxTokens { get; set; } = 150;

    // Ollama Config
    public string OllamaBaseUrl { get; set; } = "http://ollama:11434";
    public string OllamaChatModel { get; set; } = "qwen2.5:3b";

    // OpenAI Config
    public string OpenAiApiKey { get; set; } = string.Empty;
    public string OpenAiChatModel { get; set; } = "gpt-4o-mini";
}