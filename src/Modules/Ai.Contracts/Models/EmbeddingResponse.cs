namespace Ai.Contracts.Models;

public sealed record EmbeddingResponse(
    float[] Vector,
    int Dimensions,
    int InputTokens,
    string Model);