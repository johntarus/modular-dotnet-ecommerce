using MediatR;

namespace Catalog.Features.Products.Ask;

public record AskProductsQuery(string Question) : IRequest<AskProductsResult>;