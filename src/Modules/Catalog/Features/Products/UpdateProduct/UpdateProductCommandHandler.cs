using BuildingBlocks.Exceptions;
using Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Features.Products.UpdateProduct;

internal class UpdateProductCommandHandler(CatalogDbContext context)
    : IRequestHandler<UpdateProductCommand>
{
    public async Task Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id && !p.IsDeleted, ct);

        if (product is null)
        {
            throw new NotFoundException($"Product with Id {request.Id} not found.");
        }

        product.UpdateDetails(request.Name, request.Description, request.CategoryId);
        product.UpdatePrice(request.Price);

        context.Products.Update(product);
        await context.SaveChangesAsync(ct);
    }
}
