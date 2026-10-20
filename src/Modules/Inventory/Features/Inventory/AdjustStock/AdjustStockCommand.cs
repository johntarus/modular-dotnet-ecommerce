using MediatR;

namespace Inventory.Features.Inventory.AdjustStock;

public record AdjustStockCommand(
    Guid ProductId,
    int QuantityChange
    ) : IRequest;