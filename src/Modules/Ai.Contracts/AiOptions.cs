namespace Ai.Contracts;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    // Global Settings
    public string DefaultChatProvider { get; set; } = "Ollama";
    public string DefaultEmbeddingProvider { get; set; } = "Ollama"; // Locked to DB schema
    
    public int EmbeddingDimensions { get; set; } = 1024;
    public int ChatMaxTokens { get; set; } = 150;

    // Ollama Config
    public string OllamaBaseUrl { get; set; } = "http://ollama:11434";
    public string OllamaChatModel { get; set; } = "qwen2.5:3b";
    public string OllamaEmbeddingModel { get; set; } = "mxbai-embed-large"; 

    // OpenAI Config
    public string OpenAiApiKey { get; set; } = string.Empty;
    public string OpenAiChatModel { get; set; } = "gpt-4o-mini";
    public string OpenAiEmbeddingModel { get; set; } = "text-embedding-3-small";
}