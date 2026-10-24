using Grpc.Core;
using Inventory.Contracts.Grpc;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Features.Inventory.Grpc;

public class InventoryGrpcService(InventoryDbContext _context, ILogger<InventoryGrpcService> logger)
    : InventoryGrpc.InventoryGrpcBase
{
    public override async Task<CheckAvailabilityResponse> CheckAvailability(CheckAvailabilityRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.ProductId, out var productId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid ProductId format"));
        }

        var item = await _context.InventoryItems.AsNoTracking()
            .FirstOrDefaultAsync(i => i.ProductId == productId, context.CancellationToken);
        if (item is null)
        {
            return new CheckAvailabilityResponse
            {
                IsAvailable = false,
                AvailableQuantity = 0,
                Message = "Product not found in Inventory."
            };
        }

        var isAvailable = item.AvailableQuantity >= request.RequestedQuantity;
        logger.LogInformation(
            "Checked availability for {ProductId}. Requested: {Requested}, Available: {Available}, IsAvailable: {IsAvalable}",
            productId, request.RequestedQuantity, item.AvailableQuantity, isAvailable);
        return new CheckAvailabilityResponse
        {
            IsAvailable = isAvailable,
            AvailableQuantity = item.AvailableQuantity,
            Message = isAvailable ? "Stock is Available." : "Insufficient stock."
        };
    }
}