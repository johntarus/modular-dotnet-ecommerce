using Cart.Domain.Entities;
using FluentValidation;

namespace Cart.Features.Carts.SetItemQuantity;

internal sealed class SetItemQuantityCommandValidator : AbstractValidator<SetItemQuantityCommand>
{
    public SetItemQuantityCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).InclusiveBetween(0, CartItem.MaxQuantity);
    }
}