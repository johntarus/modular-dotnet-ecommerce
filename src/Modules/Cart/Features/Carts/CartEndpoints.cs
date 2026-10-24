using Cart.Features.Carts.AddItem;
using Cart.Features.Carts.GetCart;
using Cart.Features.Carts.SetItemQuantity;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Cart.Features.Carts;

public static class CartEndpoints
{
    public static RouteGroupBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/cart").WithTags("cart");
        group.MapPost("/{userId:guid}/items", AddItemToCart);
        group.MapGet("/{userId:guid}", GetCart);
        group.MapPut("/{userId:guid}/items/{productId:guid}", UpdateItemQuantity);
        return group;
    }

    private static async Task<IResult> AddItemToCart([FromBody] AddItemToCartRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new AddItemToCartCommand(request.UserId, request.ProductId, request.Quantity);
        var cartId = await sender.Send(command, ct);
        return Results.Ok(new { CartId = cartId, Message = "Item added to cart successfully" });
    }

    private static async Task<IResult> GetCart(Guid userId, ISender sender, CancellationToken ct)
    {
        var cart = await sender.Send(new GetCartQuery(userId), ct);
        return Results.Ok(cart);
    }

    private static async Task<IResult> UpdateItemQuantity(Guid userId, Guid productId,
        [FromBody] UpdateQuantityRequest request, ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new SetItemQuantityCommand(userId, productId, request.Quantity), ct);
        return Results.NoContent();
    }
}