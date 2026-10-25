using MediatR;

namespace Inventory.Features.Inventory.SetStock;

public record SetStockRequest(int QuantityChange, string Reason) : IRequest;