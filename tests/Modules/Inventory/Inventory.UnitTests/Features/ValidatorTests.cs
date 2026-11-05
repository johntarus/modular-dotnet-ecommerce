using FluentValidation.TestHelper;
using Inventory.Features.Inventory.AdjustStock;
using Inventory.Features.Inventory.SetStock;

namespace Inventory.UnitTests.Features;

public class AdjustStockCommandValidatorTests
{
    private readonly AdjustStockCommandValidator _validator = new();

    [Theory]
    [InlineData(5)]
    [InlineData(-5)]
    public void ValidCommand_HasNoErrors(int change)
    {
        _validator.TestValidate(new AdjustStockCommand(Guid.NewGuid(), change, "recount"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyProductId_HasError()
    {
        _validator.TestValidate(new AdjustStockCommand(Guid.Empty, 1, "recount"))
            .ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Fact]
    public void ZeroChange_HasError()
    {
        _validator.TestValidate(new AdjustStockCommand(Guid.NewGuid(), 0, "recount"))
            .ShouldHaveValidationErrorFor(x => x.QuantityChange);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void MissingReason_HasError(string? reason)
    {
        _validator.TestValidate(new AdjustStockCommand(Guid.NewGuid(), 1, reason!))
            .ShouldHaveValidationErrorFor(x => x.Reason);
    }
}

public class SetStockCommandValidatorTests
{
    private readonly SetStockCommandValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    public void ValidCommand_HasNoErrors(int quantity)
    {
        _validator.TestValidate(new SetStockCommand(Guid.NewGuid(), quantity, "recount"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyProductId_HasError()
    {
        _validator.TestValidate(new SetStockCommand(Guid.Empty, 1, "recount"))
            .ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Fact]
    public void NegativeQuantity_HasError()
    {
        _validator.TestValidate(new SetStockCommand(Guid.NewGuid(), -1, "recount"))
            .ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void MissingReason_HasError(string? reason)
    {
        _validator.TestValidate(new SetStockCommand(Guid.NewGuid(), 1, reason!))
            .ShouldHaveValidationErrorFor(x => x.Reason);
    }
}
