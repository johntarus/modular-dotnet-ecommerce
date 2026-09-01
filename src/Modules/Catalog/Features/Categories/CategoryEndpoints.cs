using Catalog.Features.Categories.CreateCategory;
using Catalog.Features.Categories.DeleteCategory;
using Catalog.Features.Categories.GetCategories;
using Catalog.Features.Categories.GetCategoryById;
using Catalog.Features.Categories.UpdateCategory;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Features.Categories;

public static class CategoryEndpoints
{
    public static RouteGroupBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/categories")
            .WithTags("Categories");
        group.MapPost("/", CreateCategory);
        group.MapGet("/{id:guid}", GetCategoryById);
        group.MapGet("/", GetCategories);
        group.MapPut("/{id:guid}", UpdateCategory);
        group.MapDelete("/{id:guid}", DeleteCategory);
        return group;
    }
    private static async Task<IResult> CreateCategory(
        CreateCategoryCommand command,
        ISender sender,
        CancellationToken ct)
    {
        var id = await sender.Send(command, ct);
        return Results.Created($"/api/categories/{id}", id);
    }

    private static async Task<IResult> GetCategoryById(Guid id, [FromServices] ISender sender, CancellationToken ct)
    {
        var category = await sender.Send(new GetCategoryByIdQuery(id), ct);
        return Results.Ok(category);
    }
    private static async Task<IResult> GetCategories([AsParameters] GetCategoriesQuery query,
        [FromServices] ISender sender,
        CancellationToken ct)
    {
        var categories = await sender.Send(query, ct);
        return Results.Ok(categories);
    }

    private static async Task<IResult> UpdateCategory(Guid id,
        [FromBody] UpdateCategoryRequest request,
        ISender sender,
        CancellationToken ct
    )
    {
        var command = new UpdateCategoryCommand(id, request.Name, request.Description);
        await sender.Send(command, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteCategory(Guid id,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new DeleteCategoryCommand(id), ct);
        return Results.NoContent();
    }
}