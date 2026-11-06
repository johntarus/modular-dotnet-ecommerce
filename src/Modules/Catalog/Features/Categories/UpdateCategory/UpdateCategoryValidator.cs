using Catalog.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Features.Categories.UpdateCategory;

internal sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator(CatalogDbContext context)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("CategoryId is required");
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Name must be between 1 and 200 characters");
        RuleFor(x => x.Name)
            .MustAsync(async (command, name, ct) =>
            {
                // Trimmed + case-insensitive on both sides; Trim/ToLower translate to btrim/lower in Postgres
                var key = name.Trim().ToLower();
                return !await context.Categories.AnyAsync(c => c.Id != command.Id && c.Name.Trim().ToLower() == key, ct);
            })
            .When(x => !string.IsNullOrWhiteSpace(x.Name))
            .WithMessage("A category with this name already exists.");
        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description must be between 1 and 500 characters");
    }
}
