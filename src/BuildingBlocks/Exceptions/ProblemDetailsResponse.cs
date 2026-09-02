namespace BuildingBlocks.Exceptions;

public sealed class ProblemDetailsResponse
{
    public string? Type { get; init; }
    public string? Title { get; init; }
    public int Status { get; init; }
    public string? Detail { get; init; }
    public string? TraceId { get; set; }
    public IEnumerable<string>? Errors { get; init; }
}
