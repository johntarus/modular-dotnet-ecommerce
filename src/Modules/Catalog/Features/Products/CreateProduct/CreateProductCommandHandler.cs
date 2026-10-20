using Catalog.Domain.Entities;
using Catalog.Infrastructure.Persistence;
using MediatR;

namespace Catalog.Features.Products.CreateProduct;

internal class CreateProductCommandHandler(CatalogDbContext context)
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
        
        // The Outbox message is saved within this transaction
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return product.Id;
    }
}