using BuildingBlocks.Shared;
using MediatR;

namespace Catalog.Features.Categories.GetCategories;

public sealed record GetCategoriesQuery(
    int Page = 1, 
    int PageSize = 20
    ) : IRequest<PagedResult<CategoryResponse>>;