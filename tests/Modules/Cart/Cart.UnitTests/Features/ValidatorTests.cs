using Cart.Domain.Entities;
using Cart.Features.Carts.AddItem;
using Cart.Features.Carts.SetItemQuantity;
using FluentValidation.TestHelper;

namespace Cart.UnitTests.Features;

public class AddItemToCartCommandValidatorTests
{
    private readonly AddItemToCartCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(new AddItemToCartCommand(Guid.NewGuid(), 1));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyProductId_HasError()
    {
        var result = _validator.TestValidate(new AddItemToCartCommand(Guid.Empty, 1));

        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void NonPositiveQuantity_HasError(int quantity)
    {
        var result = _validator.TestValidate(new AddItemToCartCommand(Guid.NewGuid(), quantity));

        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }
}

public class SetItemQuantityCommandValidatorTests
{
    private readonly SetItemQuantityCommandValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(CartItem.MaxQuantity)]
    public void QuantityWithinRange_HasNoErrors(int quantity)
    {
        var result = _validator.TestValidate(new SetItemQuantityCommand(Guid.NewGuid(), quantity));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyProductId_HasError()
    {
        var result = _validator.TestValidate(new SetItemQuantityCommand(Guid.Empty, 1));

        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(CartItem.MaxQuantity + 1)]
    public void QuantityOutOfRange_HasError(int quantity)
    {
        var result = _validator.TestValidate(new SetItemQuantityCommand(Guid.NewGuid(), quantity));

        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }
}
