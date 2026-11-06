using Catalog.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Features.Products.UpdateProduct;

internal sealed class UpdateProductValidator
    : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator(CatalogDbContext context)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Name)
            .MustAsync(async (command, name, ct) =>
            {
                // Trimmed + case-insensitive on both sides; Trim/ToLower translate to btrim/lower in Postgres
                var key = name.Trim().ToLower();
                return !await context.Products.AnyAsync(p => p.Id != command.Id && p.CategoryId == command.CategoryId && p.Name.Trim().ToLower() == key, ct);
            })
            .When(x => !string.IsNullOrWhiteSpace(x.Name))
            .WithMessage("A product with this name already exists in this category.");

        RuleFor(x => x.Description)
            .NotNull()
            .MaximumLength(1000);

        RuleFor(x => x.Price)
            .GreaterThan(0);

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .MustAsync(async (categoryId, ct) =>
                await context.Categories.AnyAsync(c => c.Id == categoryId, ct))
            .WithMessage("Category with the specified Id does not exist.");
    }
}
