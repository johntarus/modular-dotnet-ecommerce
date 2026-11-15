namespace Ai.Contracts;

/// <summary>The chat providers the app can route to, by the names callers use to pick one.</summary>
public static class LlmProviders
{
    public const string Ollama = "Ollama";
    public const string OpenAI = "OpenAI";

    public static IReadOnlyList<string> All { get; } = [Ollama, OpenAI];

    /// <summary>The canonical provider name matching <paramref name="name"/> ignoring case, or null if none does.</summary>
    public static string? Normalize(string? name) =>
        All.FirstOrDefault(p => string.Equals(p, name?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string UnknownProviderMessage(string? name) =>
        $"Unknown AI provider '{name}'. Supported providers: {string.Join(", ", All)}.";
}
