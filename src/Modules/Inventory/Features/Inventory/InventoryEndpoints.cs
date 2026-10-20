using Inventory.Features.Inventory.AdjustStock;
using Inventory.Features.Inventory.GetInventory;
using Inventory.Features.Inventory.GetInventoryById;
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
        // group.MapPost("/", CreateInventory);
        group.MapGet("/{id:guid}", GetInventoryById);
        group.MapGet("/", GetInventories);
        group.MapPut("/{id:guid}", AdJustStock);
        // group.MapDelete("/{id:guid}", Inventory.DeleteInventory);
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
        var command = new AdjustStockCommand(id, request.QuantityChange);
        await sender.Send(command, ct);
        return Results.NoContent();
    }
}