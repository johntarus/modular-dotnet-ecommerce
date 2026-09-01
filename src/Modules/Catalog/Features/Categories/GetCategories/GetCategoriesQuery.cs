using MediatR;

namespace Catalog.Features.Categories.GetCategories;

public sealed record GetCategoriesQuery(
    int Page = 1, 
    int PageSize = 10
    ) : IRequest<List<CategoryResponse>>;