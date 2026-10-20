using BuildingBlocks.Exceptions;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Features.Inventory.GetInventoryById;

internal class GetInventoryByIdQueryHandler(InventoryDbContext _context)
    : IRequestHandler<GetInventoryByIdQuery, InventoryResponse>
{
    public async Task<InventoryResponse> Handle(GetInventoryByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems
            .AsNoTracking()
            .Where(i => i.ProductId == request.ProductId)
            .Select(i =>
                new InventoryResponse(i.ProductId, i.QuantityOnHand, i.QuantityReserved, i.AvailableQuantity,
                    i.Updated))
            .FirstOrDefaultAsync(cancellationToken);

        if (item == null)
            throw new NotFoundException($"Inventory record with the id'{request.ProductId}' was not found");
        return item;
    }
}