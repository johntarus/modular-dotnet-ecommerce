using Catalog.Features.Products.CreateProduct;
using Catalog.Features.Products.GetProducts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Features.Products;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products")
            .WithTags("Products");
        group.MapPost(
            "/",
            CreateProduct);
        group.MapGet(
            "/",
            GetProducts);

        return group;
    }

    private static async Task<IResult> CreateProduct(
        CreateProductCommand command,
        ISender sender,
        CancellationToken ct)
    {
        var id = await sender.Send(command, ct);

        return Results.Created(
            $"/api/catalog/products/{id}",
            id);
    }

    private static async Task<IResult> GetProducts(
        [AsParameters] GetProductsQuery query,
        ISender sender,
        CancellationToken ct)
    {
        var products = await sender.Send(query, ct);
        return Results.Ok(products);
    }
}