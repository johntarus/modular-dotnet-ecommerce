using MediatR;

namespace Inventory.Features.Inventory.SetStock;

public record SetStockCommand(
    Guid ProductId,
    int QuantityChange,
    string Reason
    ) : IRequest;