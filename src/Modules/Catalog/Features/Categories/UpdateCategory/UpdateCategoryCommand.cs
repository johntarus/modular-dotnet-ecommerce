using MediatR;

namespace Catalog.Features.Categories.UpdateCategory;

public record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string? Description
    ) : IRequest;