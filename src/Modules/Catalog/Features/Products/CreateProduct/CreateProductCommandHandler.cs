using Catalog.Domain.Entities;
using Catalog.Infrastructure.Persistence;
using MediatR;

namespace Catalog.Features.Products.CreateProduct;

public class CreateProductCommandHandler(CatalogDbContext context)
    : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var product = Product.Create(
            request.Name,
            request.Description,
            request.Price,
            request.CategoryId);

        context.Products.Add(product);
        await context.SaveChangesAsync(ct);

        return product.Id;
    }
}