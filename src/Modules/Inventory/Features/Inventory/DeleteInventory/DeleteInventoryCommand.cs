using MediatR;

namespace Inventory.Features.Inventory.DeleteInventory;

public sealed record DeleteInventoryCommand(
    Guid Id) : IRequest;