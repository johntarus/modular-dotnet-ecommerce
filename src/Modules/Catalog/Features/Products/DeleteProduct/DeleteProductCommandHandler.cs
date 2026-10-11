using BuildingBlocks.Exceptions;
using Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Features.Products.DeleteProduct;

internal class DeleteProductCommandHandler(CatalogDbContext context)
    : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

        if (product is null)
        {
            throw new NotFoundException($"Product with Id {request.Id} not found.");
        }

        product.Delete();
        await context.SaveChangesAsync(ct);
    }
}
