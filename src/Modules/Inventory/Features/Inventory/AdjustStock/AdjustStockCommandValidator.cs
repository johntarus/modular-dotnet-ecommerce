using FluentValidation;

namespace Inventory.Features.Inventory.AdjustStock;

internal sealed class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("ProductId is required");
        RuleFor(x => x.QuantityChange)
            .NotEqual(0)
            .WithMessage(
                "Quantity change cannot be zero. Use a positive number to add stock or a negative number to remove stock.");
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("A reason is required.");
    }
}