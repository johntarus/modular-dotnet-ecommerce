using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Features.Inventory.AdjustStock;

internal class AdjustStockCommandHandler(
    InventoryDbContext _context
) : IRequestHandler<AdjustStockCommand>
{
    public async Task Handle(
        AdjustStockCommand request,
        CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems
            .FirstOrDefaultAsync(
                i => i.ProductId == request.ProductId && !i.IsDeleted,
                cancellationToken);

        if (item is null)
        {
            throw new KeyNotFoundException(
                $"Inventory record for product {request.ProductId} not found.");
        }

        item.AdjustStock(
            request.QuantityChange);

        await _context.SaveChangesAsync(cancellationToken);
    }
}