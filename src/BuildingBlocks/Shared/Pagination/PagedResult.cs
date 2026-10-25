namespace BuildingBlocks.Shared;

public sealed record PagedResult<T>(
    int PageNumber,
    int PageSize,
    long TotalCount,
    long TotalPages,
    bool HasPreviousPage,
    bool HasNextPage,
    IReadOnlyList<T> Items);