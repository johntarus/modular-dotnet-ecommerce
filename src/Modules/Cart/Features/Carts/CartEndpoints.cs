using Cart.Features.Carts.AddItem;
using Cart.Features.Carts.GetCart;
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
        group.MapPost("items", AddItemToCart);
        group.MapGet("/{userId:guid}", GetCart);
        return group;
    }

    private static async Task<IResult> AddItemToCart([FromBody] AddItemToCartRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new AddItemToCartCommand(request.UserId, request.ProductId, request.Quantity);
        var cartId = await sender.Send(command, ct);
        return Results.Ok(new {CartId = cartId, Message = "Item added to cart successfully" });
    }
    
    private static async Task<IResult> GetCart(Guid userId, ISender sender, CancellationToken ct)
    {
        var cart = await sender.Send(new GetCartQuery(userId), ct);
        return Results.Ok(cart);
    }
}