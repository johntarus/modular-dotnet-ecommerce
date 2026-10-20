using MediatR;

namespace Inventory.Features.Inventory.CreateInventory;

public record CreateInventoryCommand(
    string Name,
    string? Description
    ) : IRequest<Guid>;