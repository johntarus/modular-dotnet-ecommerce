using MediatR;

namespace Inventory.Features.Inventory.SetStock;

public record SetStockCommand(
    Guid ProductId,
    int Quantity,
    string Reason
    ) : IRequest;