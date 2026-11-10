namespace Ai.Contracts;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string Provider { get; set; } = "Ollama";   // "OpenAI" | "Ollama" | "Azure"
    public string ApiKey { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    public int EmbeddingDimensions { get; set; } = 1024;
    public string ChatModel { get; set; } = "qwen2.5:3b";
    public int ChatMaxTokens { get; set; } = 500;
    public string OllamaBaseUrl { get; set; } = "http://ollama:11434";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}