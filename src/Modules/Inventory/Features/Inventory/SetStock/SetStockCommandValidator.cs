using FluentValidation;
using Inventory.Features.Inventory.AdjustStock;

namespace Inventory.Features.Inventory.SetStock;

public class SetStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public SetStockCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("ProductId is required");
        RuleFor(x => x.QuantityChange)
            .NotEqual(0)
            .WithMessage(
                "Quantity change cannot be zero. Use a positive number to add stock or a negative number to remove stock.");
    }
}