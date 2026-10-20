using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Features.Inventory.GetInventory;

internal class GetInventoryQueryHandler(InventoryDbContext _context)
    : IRequestHandler<GetInventoryQuery, List<InventoryResponse>>
{
    public async Task<List<InventoryResponse>> Handle(GetInventoryQuery request, CancellationToken cancellationToken)
    {
        return await _context.InventoryItems
            .AsNoTracking()
            .OrderBy(i => i.ProductId)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i =>
                new InventoryResponse(i.ProductId, i.QuantityOnHand, i.QuantityReserved, i.AvailableQuantity,
                    i.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}