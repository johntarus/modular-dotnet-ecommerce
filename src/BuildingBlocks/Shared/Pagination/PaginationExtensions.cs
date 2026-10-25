using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Shared.Pagination;

public static class PaginationExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PaginationRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.SafePage;
        var pageSize = request.SafePageSize;

        var totalCount = await query.LongCountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (totalCount + pageSize - 1L) / pageSize;

        var items = await query
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(
            page,
            pageSize,
            totalCount,
            totalPages,
            page > 1,
            page < totalPages,
            items);
    }
}