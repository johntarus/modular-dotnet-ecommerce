using Catalog.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Features.Categories.CreateCategory;

internal sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator(CatalogDbContext context)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(x => x.Name)
            .MustAsync(async (name, ct) =>
            {
                // Trimmed + case-insensitive on both sides; Trim/ToLower translate to btrim/lower in Postgres
                var key = name.Trim().ToLower();
                return !await context.Categories.AnyAsync(c => c.Name.Trim().ToLower() == key, ct);
            })
            .When(x => !string.IsNullOrWhiteSpace(x.Name))
            .WithMessage("A category with this name already exists.");
        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}
