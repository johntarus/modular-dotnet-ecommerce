namespace Ai.Contracts.Models;

public sealed record ChatResponse(
    string Content,
    int InputTokens,
    int OutputTokens,
    string Model);