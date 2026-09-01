namespace Catalog.Features.Categories.GetCategories;

public record CategoryResponse(
    Guid Id,
    string Name,
    string? Description);