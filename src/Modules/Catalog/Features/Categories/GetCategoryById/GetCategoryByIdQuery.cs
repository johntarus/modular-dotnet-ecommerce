using MediatR;

namespace Catalog.Features.Categories.GetCategoryById;

public sealed record GetCategoryByIdQuery(
    Guid Id
) : IRequest<CategoryResponse>;