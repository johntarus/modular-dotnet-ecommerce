using MediatR;

namespace Catalog.Features.Categories.GetCategoryById;

public sealed record GetCategoryByIdQuery(
    Guid Id,
    int Page = 1, 
    int PageSize = 10
    ) : IRequest<CategoryResponse>;