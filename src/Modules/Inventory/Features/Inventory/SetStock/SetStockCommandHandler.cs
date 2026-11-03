using BuildingBlocks.Exceptions;
using Inventory.Features.Inventory.AdjustStock;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Features.Inventory.SetStock;

internal class SetStockCommandHandler(
    InventoryDbContext _context
) : IRequestHandler<SetStockCommand>
{
    public async Task Handle(
        SetStockCommand request,
        CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems
            .FirstOrDefaultAsync(
                i => i.ProductId == request.ProductId && !i.IsDeleted,
                cancellationToken);

        if (item is null)
        {
            throw new NotFoundException(
                $"Inventory record for product {request.ProductId} not found.");
        }

        item.SetStock(
            request.Quantity, request.Reason);

        await _context.SaveChangesAsync(cancellationToken);
    }
}