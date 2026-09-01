using MediatR;

namespace Catalog.Features.Categories.CreateCategory;

public record CreateCategoryCommand(
    string Name,
    string? Description
    ) : IRequest<Guid>;