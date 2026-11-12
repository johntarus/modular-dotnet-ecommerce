using Catalog.Features.Products.CreateProduct;
using Catalog.Features.Products.DeleteProduct;
using Catalog.Features.Products.GetProductById;
using Catalog.Features.Products.GetProducts;
using Catalog.Features.Products.UpdateProduct;
using BuildingBlocks.Infrastructure.Auth;
using Catalog.Features.Products.Ask;
using Catalog.Features.Products.SemanticSearch;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            CreateProduct).RequireAuthorization(AuthPolicies.CanManageCatalog);
        group.MapGet(
            "/",
            GetProducts);
        group.MapGet(
            "/{id:guid}",
            GetProductById);
        group.MapPut(
            "/{id:guid}",
            UpdateProduct).RequireAuthorization(AuthPolicies.CanManageCatalog);
        group.MapDelete(
            "/{id:guid}",
            DeleteProduct).RequireAuthorization(AuthPolicies.CanManageCatalog);
        group.MapPost("/search-semantic", Search);
        group.MapPost("/ask", Ask);


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
    private static async Task<IResult> GetProductById(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        var product = await sender.Send(new GetProductByIdQuery(id), ct);
        return Results.Ok(product);
    }

    private static async Task<IResult> UpdateProduct(
        Guid id,
        UpdateProductCommand command,
        ISender sender,
        CancellationToken ct)
    {
        var updateCommand = command with { Id = id };
        await sender.Send(updateCommand, ct);

        // Fetch and return the updated product to verify
        var product = await sender.Send(new GetProductByIdQuery(id), ct);
        return Results.Ok(product);
    }

    private static async Task<IResult> DeleteProduct(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new DeleteProductCommand(id), ct);
        return Results.NoContent();
    }
    
    private static async Task<IResult> Search(
        [FromBody] SemanticSearchRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var results = await sender.Send(new SemanticSearchQuery(request.Query, request.Limit), ct);

        return Results.Ok(new
        {
            request.Query,
            results.Count,
            Results = results
        });
    }

    private static async Task<IResult> Ask(AskProductsQuery productsQuery, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new AskProductsQuery(productsQuery.Question), ct);
        return Results.Ok(result);
    }
}