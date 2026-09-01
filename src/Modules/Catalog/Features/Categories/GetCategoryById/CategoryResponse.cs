namespace Catalog.Features.Categories.GetCategoryById;

public record CategoryResponse(
    Guid Id,
    string Name,
    string? Description);