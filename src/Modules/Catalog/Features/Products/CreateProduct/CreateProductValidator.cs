using Catalog.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Features.Products.CreateProduct;

public sealed class CreateProductValidator
    : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator(CatalogDbContext context)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Price)
            .GreaterThan(0);

        RuleFor(x => x.CategoryId)
            .NotEmpty();
        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .MustAsync(async (categoryId, ct) =>
                await context.Categories.AnyAsync(c => c.Id == categoryId, ct))
            .WithMessage("Category with the specified Id does not exist.");
    }
}