namespace Catalog.Features.Categories.UpdateCategory;

public record UpdateCategoryRequest(
    string Name,
    string? Description);