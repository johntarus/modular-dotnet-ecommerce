using Catalog.Features.Categories.CreateCategory;
using FluentValidation;

namespace Catalog.Features.Categories.UpdateCategory;

public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("CategoryId is required");
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Name must be between 1 and 200 characters");
        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description must be between 1 and 500 characters");
    }
}