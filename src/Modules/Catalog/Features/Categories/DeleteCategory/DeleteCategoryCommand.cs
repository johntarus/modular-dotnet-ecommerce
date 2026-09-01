using MediatR;

namespace Catalog.Features.Categories.DeleteCategory;

public sealed record DeleteCategoryCommand(
    Guid Id) : IRequest;