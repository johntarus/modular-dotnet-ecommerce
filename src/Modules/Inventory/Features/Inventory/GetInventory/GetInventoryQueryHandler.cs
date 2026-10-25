using BuildingBlocks.Shared;
using BuildingBlocks.Shared.Pagination;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Features.Inventory.GetInventory;

internal class GetInventoryQueryHandler(InventoryDbContext _context)
    : IRequestHandler<GetInventoryQuery, PagedResult<InventoryResponse>>
{
    public async Task<PagedResult<InventoryResponse>> Handle(GetInventoryQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.InventoryItems
            .AsNoTracking()
            .OrderBy(i => i.ProductId)
            .Select(i =>
                new InventoryResponse(i.ProductId, i.QuantityOnHand, i.QuantityReserved, i.AvailableQuantity,
                    i.UpdatedAt));

        return await query.ToPagedResultAsync(new PaginationRequest(request.Page, request.PageSize), cancellationToken);
    }
}