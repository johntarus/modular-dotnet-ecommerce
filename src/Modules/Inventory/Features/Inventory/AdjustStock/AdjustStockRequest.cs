using MediatR;

namespace Inventory.Features.Inventory.AdjustStock;

public record AdjustStockRequest(int QuantityChange, string Reason) : IRequest;