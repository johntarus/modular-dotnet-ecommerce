using FluentValidation;

namespace Cart.Features.Carts.AddItem;

public class AddItemToCartCommandValidator
    : AbstractValidator<AddItemToCartCommand>
{
    public AddItemToCartCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty();

        RuleFor(x => x.Quantity)
            .GreaterThan(0);
    }
}