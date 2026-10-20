using MediatR;

namespace Inventory.Features.Inventory.GetInventoryById;

public sealed record GetInventoryByIdQuery(
    Guid ProductId
) : IRequest<InventoryResponse>;