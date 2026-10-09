using MediatR;
using System.Text.Json.Serialization;

namespace Catalog.Features.Products.UpdateProduct;

public sealed record UpdateProductCommand(
    string Name,
    string Description,
    decimal Price,
    Guid CategoryId
) : IRequest
{
    [JsonIgnore]
    public Guid Id { get; init; }
}
