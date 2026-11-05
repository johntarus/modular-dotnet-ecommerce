using Cart.Features.Carts.AddItem;
using Cart.Features.Carts.ClearCart;
using Cart.Features.Carts.GetCart;
using Cart.Features.Carts.MergeGuestCart;
using Cart.Features.Carts.RemoveItem;
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

        group.MapGet("/", GetCart);
        group.MapPost("/items", AddItemToCart);
        group.MapPut("/items/{productId:guid}", UpdateItemQuantity);
        group.MapDelete("/items/{productId:guid}", RemoveItem);
        group.MapDelete("/cart", ClearCart);

        group.MapPost("/merge", MergeGuestCart).RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetCart(ISender sender, CancellationToken ct)
    {
        var cart = await sender.Send(new GetCartQuery(), ct);
        return Results.Ok(cart);
    }

    private static async Task<IResult> AddItemToCart(
        HttpContext httpContext,
        [FromHeader(Name = "X-Cart-Token")] string? guestToken,
        [FromBody] AddItemToCartRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new AddItemToCartCommand(request.ProductId, request.Quantity), ct);
        if(result.IssuedGuestToken is not null)
            httpContext.Response.Headers["X-Cart-Token"] = result.IssuedGuestToken;
        return Results.Ok(new
        {
            result.CartId,
            IssuedGuestToken = result.IssuedGuestToken,
            Message = "Item added to cart successfully"
        });
    }

    private static async Task<IResult> UpdateItemQuantity(
        Guid productId, [FromBody] UpdateQuantityRequest request, ISender sender, CancellationToken ct)
    {
        await sender.Send(new SetItemQuantityCommand(productId, request.Quantity), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveItem(Guid productId, ISender sender, CancellationToken ct)
    {
        await sender.Send(new RemoveItemCommand(productId), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ClearCart(ISender sender, CancellationToken ct)
    {
        await sender.Send(new ClearCartCommand(), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> MergeGuestCart([FromHeader(Name = "X-Cart-Token")] string? guestToken, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new MergeGuestCartCommand(guestToken),  ct);
        return Results.Ok(result);
    }
}