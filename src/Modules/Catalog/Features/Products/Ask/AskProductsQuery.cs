using MediatR;

namespace Catalog.Features.Products.Ask;

public record AskProductsQuery(string Question, string? providerName = null) : IRequest<AskProductsResult>;