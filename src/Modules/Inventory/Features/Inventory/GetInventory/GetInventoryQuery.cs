using MediatR;

namespace Inventory.Features.Inventory.GetInventory;

public sealed record GetInventoryQuery(
    int Page = 1, 
    int PageSize = 10
    ) : IRequest<List<InventoryResponse>>;