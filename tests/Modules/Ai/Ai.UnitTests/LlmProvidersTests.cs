using Ai.Contracts;

namespace Ai.UnitTests;

public class LlmProvidersTests
{
    [Theory]
    [InlineData("Ollama", "Ollama")]
    [InlineData("ollama", "Ollama")]
    [InlineData(" OPENAI ", "OpenAI")]
    [InlineData("Gemini", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Normalize_ReturnsCanonicalNameOrNull(string? input, string? expected)
    {
        Assert.Equal(expected, LlmProviders.Normalize(input));
    }
}
