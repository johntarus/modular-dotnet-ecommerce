using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Features.Inventory.DeleteInventory;

internal class DeleteInventoryCommandHandler(InventoryDbContext _context) : IRequestHandler<DeleteInventoryCommand>
{
    public async Task Handle(DeleteInventoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == request.Id, cancellationToken);
        if (category is null) throw new KeyNotFoundException($"Category {request.Id} was not found");
        category.Delete();
        await _context.SaveChangesAsync(cancellationToken);
    }
}