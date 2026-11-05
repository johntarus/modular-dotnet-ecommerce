using BuildingBlocks.Infrastructure.Auth;
using Inventory.Features.Inventory.AdjustStock;
using Inventory.Features.Inventory.GetInventory;
using Inventory.Features.Inventory.GetInventoryById;
using Inventory.Features.Inventory.SetStock;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Inventory.Features.Inventory;

public static class InventoryEndpoints
{
    public static RouteGroupBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/inventory")
            .WithTags("Inventory");
        group.MapGet("/{id:guid}", GetInventoryById);
        group.MapGet("/", GetInventories);
        group.MapPatch("/{id:guid}/adjustments", AdJustStock).RequireAuthorization(AuthPolicies.CanAdjustStock);
        group.MapPut("/{id:guid}/stock", SetStock).RequireAuthorization(AuthPolicies.CanAdjustStock);
        return group;
    }

    private static async Task<IResult> GetInventoryById(Guid id, [FromServices] ISender sender, CancellationToken ct)
    {
        var item = await sender.Send(new GetInventoryByIdQuery(id), ct);
        return Results.Ok(item);
    }
    private static async Task<IResult> GetInventories([AsParameters] GetInventoryQuery query,
        [FromServices] ISender sender,
        CancellationToken ct)
    {
        var items = await sender.Send(query, ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> AdJustStock(Guid id,
        [FromBody] AdjustStockRequest request,
        ISender sender,
        CancellationToken ct
    )
    {
        var command = new AdjustStockCommand(id, request.QuantityChange, request.Reason);
        await sender.Send(command, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SetStock(Guid id, [FromBody] SetStockRequest request, ISender sender,
        CancellationToken ct)
    {
        var command = new SetStockCommand(id, request.Quantity, request.Reason);
        await sender.Send(command, ct);
        return Results.NoContent();
    }
}