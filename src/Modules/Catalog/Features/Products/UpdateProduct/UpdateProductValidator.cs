using Catalog.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Features.Products.UpdateProduct;

public sealed class UpdateProductValidator
    : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator(CatalogDbContext context)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Price)
            .GreaterThan(0);

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .MustAsync(async (categoryId, ct) =>
                await context.Categories.AnyAsync(c => c.Id == categoryId, ct))
            .WithMessage("Category with the specified Id does not exist.");
    }
}
