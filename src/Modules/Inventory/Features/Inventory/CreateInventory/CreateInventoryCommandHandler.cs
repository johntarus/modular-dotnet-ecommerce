// using Inventory.Domain.Entities;
// using Inventory.Infrastructure.Persistence;
// using MediatR;
//
// namespace Inventory.Features.Inventory.CreateInventory;
//
// internal class CreateInventoryCommandHandler(InventoryDbContext _context)
//     : IRequestHandler<CreateInventoryCommand, Guid>
// {
//     public async Task<Guid> Handle(CreateInventoryCommand request, CancellationToken cancellationToken)
//     {
//         var inventoryItem = InventoryItem.Create(
//             request.);
//         _context.InventoryItems.Add(inventoryItem);
//         await _context.SaveChangesAsync(cancellationToken);
//         return InventoryItem.Id;
//     }
// }