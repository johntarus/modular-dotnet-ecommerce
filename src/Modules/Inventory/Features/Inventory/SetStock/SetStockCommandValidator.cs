using FluentValidation;

namespace Inventory.Features.Inventory.SetStock;

public class SetStockCommandValidator : AbstractValidator<SetStockCommand>
{
    public SetStockCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("ProductId is required");

        // SetStock replaces QuantityOnHand with an absolute value (not a delta).
        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Quantity cannot be negative.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("A reason is required.");
    }
}
