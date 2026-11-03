using MediatR;

namespace Inventory.Features.Inventory.SetStock;

public record SetStockRequest(int Quantity, string Reason) : IRequest;