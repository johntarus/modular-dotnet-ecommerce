using FluentValidation;

namespace Orders.Features.CreateOrder;

internal sealed class CreateOrderCommandValidator
    : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CartId)
            .NotEmpty();

        RuleFor(x => x.AddressId)
            .NotEmpty();
    }
}
