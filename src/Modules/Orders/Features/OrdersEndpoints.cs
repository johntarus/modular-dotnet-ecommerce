using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Orders.Features.CreateOrder;

namespace Orders.Features;

public static class OrdersEndpoints
{
    /// <param name="group">The "/api" group — routes here are mapped relative to it.</param>
    public static RouteGroupBuilder MapOrdersEndpoints(this RouteGroupBuilder group)
    {
        // Checkout turns the user's cart into an order; the order itself lives at /api/orders/{id}.
        group.MapPost("/checkout", Checkout)
            .RequireAuthorization()
            .WithName("Checkout")
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> Checkout(
        [FromBody] CreateOrderRequest request,
        [FromServices] ISender sender,
        CancellationToken ct)
    {
        var command = new CreateOrderCommand(request.CartId, request.AddressId);

        var orderId = await sender.Send(command, ct);

        return Results.Created($"/api/orders/{orderId}", new { id = orderId });
    }
}